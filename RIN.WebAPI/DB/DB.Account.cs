using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using FauFau.Net.Web;
using RIN.WebAPI.Models.DB;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.DB
{
    public partial class DB
    {
        // Register a new account in the DB
        // Checks to make sure the email isn't already in use
        // Returns the account id and an error message if there is one
        public async Task<(long id, string errorStr)> RegisterNewAccount(string email, string password, string country, DateTime birthday, string referralKey, bool emailOpin = false)
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
                p.Add("@email_opin", emailOpin);

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
            var result = await DBCall(conn => conn.ExecuteAsync(UPDATE_SQL, new {accountId, loginTime}));

            return result > 0;
        }
    }
}