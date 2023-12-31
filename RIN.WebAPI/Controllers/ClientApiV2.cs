using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RIN.Core.DB;
using RIN.WebAPI.Models.Config;

namespace RIN.WebAPI.Controllers
{
    [ApiController]
    [Route("Clientapi/api/v2")]
    public partial class ClientApiV2 : TmwController
    {
        private readonly ServerDefaultsSettings         ServerDefaults;
        private readonly ILogger<OperatorController>    Logger;
        private readonly DB                             Db;

        public ClientApiV2(IOptions<ServerDefaultsSettings> serverDefaults, ILogger<OperatorController> logger, DB db)
        {
            ServerDefaults = serverDefaults.Value;
            Logger         = logger;
            Db             = db;
        }
        
    }
}