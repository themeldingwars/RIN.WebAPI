using System.Diagnostics;
using System.Runtime.CompilerServices;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using RIN.Core.Config;

namespace RIN.Core.DB
{
    public class DbBase
    {
        protected DbConnectionSettings Config;
        protected readonly ILogger    Logger;

        public    string ConnStr;
        protected bool   LogDbTimes;
        
        public DbBase(IOptions<DbConnectionSettings> config, ILogger<DbBase> logger)
        {
            Config        = config.Value;
            Logger        = logger;
        }
        
        private async Task<int> Execute(string sql, object prams, Action<Exception> onError = null!)
        {
            try {
                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();
                var result  = await conn.ExecuteAsync(sql, prams);
                return result;
            }
            catch (Exception e) {
                onError?.Invoke(e);
                return -1;
            }
        }

        public async Task<T> DBCall<T>(Func<NpgsqlConnection, Task<T>> context, Action<Exception> onError = null!, [CallerMemberName] string functionName = null)
        {
            try {
                var             sw   = Stopwatch.StartNew();
                await using var conn = new NpgsqlConnection(ConnStr);
                await conn.OpenAsync();
                var result = await context(conn);

                if (LogDbTimes) {
                    sw.Stop();
                    Logger.LogInformation("DB call {functionName} took {elapsed} ({ms}ms)", functionName, sw.Elapsed, sw.Elapsed.TotalMilliseconds);
                }
                
                return result;
            }
            catch (Exception e) {
                onError?.Invoke(e);
                Logger.LogError($"{functionName}: {e}");
                
                return default;
            }
        }
    }
}
