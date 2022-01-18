using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using RIN.WebAPI.Models.ClientApi;

namespace RIN.WebAPI.DB
{
    public partial class DB
    {
        
        // Log a client event from the client to the DB
        public async Task<bool> LogClientEvent(ClientEvent evnt, string userId = null!)
        {
            const string INSERT_SQL =
                "INSERT INTO webapi.\"ClientEvents\" (event, action, message, source, data, date) VALUES (@event, @action, @message, @source, @data, current_timestamp);";

            const string INSERT_SQL_WITH_USER =
                "INSERT INTO webapi.\"ClientEvents\" (event, action, message, source, data, date, user_id) VALUES (@event, @action, @message, @source, @data, current_timestamp, (SELECT account_id FROM webapi.\"Accounts\" where uid = decode(@userID, 'base64')));";

            var log = new
            {
                evnt.@event,
                evnt.action,
                evnt.data.message,
                evnt.data.source,
                evnt.data.data,
                userID = userId
            };

            bool hasUserId = userId != null && userId.Trim().Length > 0;
            var  sql       = hasUserId ? INSERT_SQL_WITH_USER : INSERT_SQL;
            
            var result = await DBCall(conn => conn.ExecuteAsync(sql, log),
                exception =>
                {
                    Logger.LogError($"Error logging client event user for {userId} due to: {exception}");
                    throw exception;
                });

            var success = result >= 0;
            return success;
        }
    }
}