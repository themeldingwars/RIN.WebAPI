using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace RIN.WebAPI.DB
{
    public class DbBase
    {
        protected readonly IConfiguration Configuration;
        protected readonly ILogger    Logger;

        protected string ConnStr;
        protected bool   LogDbTimes;
        
        public DbBase(IConfiguration configuration, ILogger<DbBase> logger)
        {
            Configuration = configuration;
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