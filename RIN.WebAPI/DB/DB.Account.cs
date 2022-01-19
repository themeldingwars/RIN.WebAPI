using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using FauFau.Net.Web;
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
    }
}