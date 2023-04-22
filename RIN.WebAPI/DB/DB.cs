using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RIN.WebAPI.Models;
using RIN.WebAPI.Models.Config;

namespace RIN.WebAPI.DB
{
    public partial class DB : DbBase
    {
        protected WebApiConfigSettings Config;

        public DB(IOptions<DbConnectionSettings> config, ILogger<DB> logger) : base(config, logger)
        {
            ConnStr    = config.Value.DBConnStr;
            LogDbTimes = config.Value.LogDbCallTimes;
        }
    }
}