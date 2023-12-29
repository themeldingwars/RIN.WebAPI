using FauFau.Net.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using RIN.WebAPI.DB;
using RIN.WebAPI.Models;
using System.Reflection.Metadata.Ecma335;
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
        private DB.DB Db;
        private SessionManager SessionManager;
        private ILogger<R5SigAuth> Logger;

        public R5SigAuth(IOptions<WebApiConfigSettings> config, DB.DB db, SessionManager sessionManager, ILogger<R5SigAuth> logger)
        {
            Config         = config.Value;
            Db             = db;
            SessionManager = sessionManager;
            Logger         = logger;
        }

        private (bool, string uid) VerifyR5SigToBody(ReadOnlySpan<char> headerStr)
        {
            var r5Sig = Red5Sig.ParseString(headerStr);
            var uid   = HttpUtility.UrlDecode(r5Sig.UID.ToString());
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!Config.VerifyRed5Sig)
                return;

            var str = context.HttpContext.Request.Headers.TryGetValue("X-Red5-Signature", out StringValues header) ? header.FirstOrDefault() : null;
            if (str == null)
            {
                Logger.LogWarning("Can't get or create session for request with no X-Red5-Signature. {Req}", null);
                context.Result = CreateError(Error.Codes.ERR_INCORRECT_USERPASS);
                return;
            }

            // verify body data
            var (verified, uid) = VerifyR5SigToBody(str);

            // check if we have a session for this uid


            // check if the auth matches

            var (errorCode, sessionData, uid) = SessionManager.GetOrCreateAtuthUserSession(context.HttpContext.Request);
            if (errorCode != null)
                context.Result = CreateError(errorCode);
            else
            {
                // If here then we are authed
                context.HttpContext.Items.Add(SessionManager.SESSION_KEY_PREFIX, sessionData);
            }

            /*
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
            */
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
