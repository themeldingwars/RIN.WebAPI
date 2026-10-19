using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RIN.Core.DB;
using RIN.Core.DB.SDB;
using RIN.WebAPI.Models.Config;

namespace RIN.WebAPI.Controllers
{
    [ApiController]
    [Route("Clientapi/api/v4")]
    public partial class ClientApiV4 : TmwController
    {
        private readonly ServerDefaultsSettings         ServerDefaults;
        private readonly ILogger<OperatorController>    Logger;
        private readonly DB                             Db;
        private readonly SDB                            SDB;

        public ClientApiV4(IOptions<ServerDefaultsSettings> serverDefaults, ILogger<OperatorController> logger, DB db, SDB sdb)
        {
            ServerDefaults = serverDefaults.Value;
            Logger         = logger;
            Db             = db;
            SDB            = sdb;
        }
    }
}