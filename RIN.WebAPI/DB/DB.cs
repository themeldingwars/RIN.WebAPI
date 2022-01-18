using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RIN.WebAPI.Models;

namespace RIN.WebAPI.DB
{
    public partial class DB : DbBase
    {
        public DB(IConfiguration configuration, ILogger<DB> logger) : base(configuration, logger)
        {
            var config = Configuration.GetSection(WebApiConfigSettings.NAME).Get<WebApiConfigSettings>();
            ConnStr    = config.SDBConnectionStr;
            LogDbTimes = config.LogDbCallTimes;
        }
    }
}