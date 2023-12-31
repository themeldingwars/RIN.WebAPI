using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RIN.Core.Config;

namespace RIN.Core.DB
{
    public partial class DB : DbBase
    {
        public DB(IOptions<DbConnectionSettings> config, ILogger<DB> logger) : base(config, logger)
        {
            ConnStr    = config.Value.DBConnStr;
            LogDbTimes = config.Value.LogDbCallTimes;
        }
    }
}