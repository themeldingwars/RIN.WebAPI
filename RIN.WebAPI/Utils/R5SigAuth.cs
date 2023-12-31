using FauFau.Net.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using RIN.Core;
using RIN.Core.DB;
using RIN.WebAPI.Models;
using System.Web;

namespace RIN.WebAPI.Utils
{
    public class R5SigAuthRequiredAttribute : TypeFilterAttribute
    {
        public R5SigAuthRequiredAttribute() : base(typeof(R5SigAuth))
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class R5SigAuth : Attribute, IAsyncAuthorizationFilter
    {
        private WebApiConfigSettings Config;
        private DB Db;

        public R5SigAuth(IOptions<WebApiConfigSettings> config, DB db)
        {
            Config = config.Value;
            Db     = db;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!Config.VerifyRed5Sig)
                return;

            var str = context.HttpContext.Request.Headers.TryGetValue("X-Red5-Signature", out StringValues header) ? header.FirstOrDefault() : null;
            if (str == null)
                context.Result = CreateError(Error.Codes.ERR_INCORRECT_USERPASS, "No Signature");

            // TODO: Verify request body with sig

            var uid = HttpUtility.UrlDecode(Red5Sig.ParseString(str).UID.ToString());

            // TODO: Cache the users uuid to account id to skip a db call
            var loginResult = await Db.GetLoginData(uid);
            if (loginResult == null)
                context.Result = CreateError(Error.Codes.ERR_INCORRECT_USERPASS);

            var authed = Auth.Verify(loginResult.secret, str.AsSpan());
            if (!authed)
                context.Result = CreateError(Error.Codes.ERR_INCORRECT_USERPASS);
        }

        private ObjectResult CreateError(string code, string msg = null)
        {
            var error         = new Error(code, msg);
            var result        = new ObjectResult(error);
            result.StatusCode = 500;

            return result;
        }
    }
}
