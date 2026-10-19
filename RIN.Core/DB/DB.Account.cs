using System;
using System.Data;
using System.Net.Security;
using System.Threading.Tasks;
using Dapper;
using FauFau.Net.Web;
using RIN.Core.DB;
using RIN.Core.Utils;

namespace RIN.Core.DB
{
    public partial class DB
    {
        // Register a new account in the DB
        // Checks to make sure the email isn't already in use
        // Returns the account id and an error message if there is one
        public async Task<(long id, string errorStr)> RegisterNewAccount(string email, string password, string country, DateTime birthday, string referralKey, bool emailOptin = false)
        {
            var uid          = Auth.GenerateUserId(email.AsSpan()).ToString();
            var secret       = Auth.GenerateSecret(email.AsSpan(), password.AsSpan()).ToString();
            var passwordHash = AuthUtil.CreatePasswordHashAndSalt(password);

            var result = await DBCall(async conn =>
            {
                var p = new DynamicParameters();
                p.Add("@email", email);
                p.Add("@uid", uid);
                p.Add("@secret", secret);
                p.Add("@password_hash", passwordHash);
                p.Add("@country", country);
                p.Add("@birthday", birthday, DbType.Date);
                p.Add("@email_optin", emailOptin);

                p.Add("@error_text", dbType: DbType.String, direction: ParameterDirection.Output);
                p.Add("@new_account_id", dbType: DbType.Int64, direction: ParameterDirection.Output);

                var r = await conn.ExecuteAsync("webapi.\"CreateNewAccount\"", p, commandType: CommandType.StoredProcedure);

                return (p.Get<long>("@new_account_id"), p.Get<string>("@error_text"));
            });

            return result;
        }

        // Get login data needed to verify a login and respond
        public async Task<LoginResult?> GetLoginData(string uid)
        {
            const string SELECT_SQL = @"SELECT
                webapi.""Accounts"".account_id,
                is_dev,
                created_at,
                secret,
                character_limit,
                true AS                                                             can_login,
                (vip.expiration_date > vip.start_date AND vip.account_id            IS NOT          NULL) AS is_vip,
                COALESCE(EXTRACT(EPOCH FROM vip.expiration_date) * 1000, -1) AS vip_expiration,
                '' AS                                                               error,
                '' AS                                                               error_msg

                FROM webapi.""Accounts""
                LEFT JOIN webapi.""VipData"" AS vip ON vip.account_id = webapi.""Accounts"".account_id
                WHERE uid = @uid";

            var result = await DBCall(async conn =>
            {
                var r = await conn.QueryFirstOrDefaultAsync<LoginResult>(SELECT_SQL, new {uid});
                return r;
            });

            return result;
        }

        // Update the last time this account logged in successfully
        public async Task<bool> UpdateLastLoginTime(long accountId, DateTime? loginTime = null)
        {
            const string UPDATE_SQL = @"update webapi.""Accounts"" set last_login = @loginTime where account_id = @accountId;";

            loginTime ??= DateTime.Now;
            var result = await DBCall( conn => conn.ExecuteAsync(UPDATE_SQL, new {accountId, loginTime}));

            return result > 0;
        }

        public async Task<bool> UpdateLanguage(long accountId, String language)
        {

            const string UPDATE_SQL = @"update webapi.""Accounts"" set language = @language where account_id = @accountId;";

            var result = await DBCall(conn => conn.ExecuteAsync(UPDATE_SQL, new { language, accountId }));

            return result > 0;
        }

        public async Task<AccountMTX> GetAccountMTXData(long accountId)
        {
            const string SELECT_SQL = @"SELECT
                rb_balance,

                (SELECT
                price as name_change_cost
                FROM webapi.""Costs""
                WHERE name = 'name_change_cost')

                FROM webapi.""Accounts""
                WHERE account_id = @accountId";

            var result = await DBCall(async conn =>
            {
                var r = await conn.QueryFirstOrDefaultAsync<AccountMTX>(SELECT_SQL, new { accountId });
                return r;
            });

            return result;
        }

        // Returns the account's ticket, minting one if it does not have one yet.
        public async Task<string> GetOrCreateLoginTicket(long accountId)
        {
            var existing = await GetLoginTicket(accountId);
            if (existing != null)
                return existing;

            for (var attempt = 0; attempt < 3; attempt++) {
                var secret = LoginTicket.GenerateSecret();

                var inserted = await DBCall(conn => conn.ExecuteAsync(
                    @"UPDATE webapi.""Accounts""
                        SET login_ticket            = @secret,
                            login_ticket_hash       = @hash,
                            login_ticket_created_at = current_timestamp
                      WHERE account_id     = @accountId
                        AND login_ticket IS NULL",
                    new { accountId, secret, hash = LoginTicket.Hash(secret) }));

                if (inserted > 0)
                    return LoginTicket.Encode(secret);

                existing = await GetLoginTicket(accountId);
                if (existing != null)
                    return existing;
            }

            return null;
        }

        // Replaces the account's ticket with a new one, invalidating the old immediately. This is for rotating by hand.
        public async Task<string> RegenerateLoginTicket(long accountId)
        {
            var secret = LoginTicket.GenerateSecret();

            var updated = await DBCall(conn => conn.ExecuteAsync(
                @"UPDATE webapi.""Accounts""
                    SET login_ticket            = @secret,
                        login_ticket_hash       = @hash,
                        login_ticket_created_at = current_timestamp
                  WHERE account_id = @accountId",
                new { accountId, secret, hash = LoginTicket.Hash(secret) }));

            return updated > 0 ? LoginTicket.Encode(secret) : null;
        }

        public async Task<string> GetLoginTicket(long accountId)
        {
            const string SELECT_SQL = @"SELECT login_ticket
                                        FROM webapi.""Accounts""
                                        WHERE account_id = @accountId
                                          AND login_ticket IS NOT NULL";

            var raw = await DBCall(conn => conn.QueryFirstOrDefaultAsync<byte[]>(SELECT_SQL, new { accountId }));

            return raw == null ? null : LoginTicket.Encode(raw);
        }

        // Takes the ticket payload the game server presented including the header. Strips the header, hashes the secret and looks the hash up. A
        // database failure is reported as Error, not Unknown: a failed lookup must not look like a bad ticket, and the caller has to deny access either way.
        public async Task<LoginTicketCheck> ValidateLoginTicket(byte[] ticketPayload)
        {
            var secret = LoginTicket.TryDecodePayload(ticketPayload);
            if (secret == null)
                return new LoginTicketCheck { Status = LoginTicketStatus.Malformed };

            const string SELECT_SQL = @"SELECT account_id, is_dev, character_limit, login_ticket
                                        FROM webapi.""Accounts""
                                        WHERE login_ticket_hash = @hash";

            var dbFailed = false;
            var row = await DBCall(conn => conn.QueryFirstOrDefaultAsync<LoginTicketRow>(SELECT_SQL, new { hash = LoginTicket.Hash(secret) }),
                                   ex => dbFailed = true);

            if (dbFailed)
                return new LoginTicketCheck { Status = LoginTicketStatus.Error };

            if (row == null)
                return new LoginTicketCheck { Status = LoginTicketStatus.Unknown };

            if (!LoginTicket.SecretsMatch(secret, row.login_ticket))
                return new LoginTicketCheck { Status = LoginTicketStatus.Unknown };

            return new LoginTicketCheck
            {
                Status  = LoginTicketStatus.Valid,
                Account = new LoginTicketAccount
                {
                    account_id      = row.account_id,
                    is_dev          = row.is_dev,
                    character_limit = row.character_limit
                }
            };
        }

        // Who a character belongs to, and whether it is still playable. Deletion is a row in DeletionQueue;
        // Characters.deleted_at is checked as well so either marker is enough to keep a character out.
        public async Task<LoginCharacter> GetLoginCharacter(long characterGuid)
        {
            const string SELECT_SQL = @"SELECT c.character_guid,
                                        c.account_id,
                                        c.is_dev,
                                        c.is_active,
                                        c.name,
                                        (c.deleted_at IS NOT NULL
                                         OR EXISTS (SELECT 1 FROM webapi.""DeletionQueue"" dq
                                                    WHERE dq.character_guid = c.character_guid)) AS is_deleted
                                        FROM webapi.""Characters"" c
                                        WHERE c.character_guid = @characterGuid";

            var result = await DBCall(conn => conn.QueryFirstOrDefaultAsync<LoginCharacter>(SELECT_SQL, new { characterGuid }));

            return result;
        }
    }
}