using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RIN.WebAPI.Models;
using RIN.WebAPI.Models.Config;
using RIN.WebAPI.Models.Operator;
using Serilog;

namespace RIN.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OperatorController : TmwController
    {
        private readonly IConfiguration Configuration;
        private readonly                 ILogger<OperatorController> Logger;
        private WebApiConfigSettings    Config => Configuration.GetSection(WebApiConfigSettings.WEB_API_CONFIG).Get<WebApiConfigSettings>();

        public OperatorController(IConfiguration configuration, ILogger<OperatorController> logger)
        {
            Logger        = logger;
            Configuration = configuration;
        }
        
        [HttpGet("/api/v1/products/Firefall_Beta")]
        public async Task<BuildInfo> BuildInfo()
        {
            var buildInfo = new BuildInfo
            {
                build       = "beta-1962",
                environment = "dev",
                region      = "EU",
                patch_level = 0
            };
            
            return buildInfo;
        }
        
        [HttpGet]
        [Route("/check")]
        public async Task<Hosts> Check([FromQuery] CheckReq args)
        {
            var baseUrl     = Config.BaseURL;

            // Todo: replace with configurable settings
            var hosts = new Hosts
            {
                frontend_host     = $"{baseUrl}/frontend",
                store_host        = $"{baseUrl}/store",
                chat_server       = $"{baseUrl}/chatserver",
                replay_host       = $"{baseUrl}/replay",
                web_host          = $"{baseUrl}/web",
                market_host       = $"{baseUrl}/market",
                ingame_host       = $"{baseUrl}/ingame",
                clientapi_host    = $"{baseUrl}/clientapi",
                web_asset_host    = $"{baseUrl}/webasset",
                rhsigscan_host    = $"{baseUrl}/rhsigscan",
                web_accounts_host = $"{baseUrl}/webaccounts"
            };

            return hosts;
        }
        
        // Log unhandled requests to look into adding later
        [HttpGet("*")]
        [HttpPost("*")]
        public async void CatchAll()
        {
            var baseDir = Path.Combine(Environment.CurrentDirectory, "../", "UnhandledRoutes");
            var dir     = baseDir + Request.Path.Value?.Replace('/', '\\');
            Directory.CreateDirectory(dir);

            var filePath = Path.Combine(dir, $"{DateTime.Now.ToFileTime()}.txt");

            var sb = new StringBuilder();
            sb.AppendLine($"URL: {Request.Path.Value}");
            sb.AppendLine($"Type: {Request.Method}");
            sb.AppendLine($"QS: {Request.QueryString.Value}");

            // Headers
            sb.AppendLine();
            sb.AppendLine($"Headers");
            foreach (var kvp in Request.Headers) {
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            
            // Body
            using (var reader = new StreamReader(Request.Body)) {
                var body = await reader.ReadToEndAsync();
                sb.AppendLine($"Body: {body}");
            }

            await System.IO.File.WriteAllTextAsync(filePath, sb.ToString());
        }
    }
}