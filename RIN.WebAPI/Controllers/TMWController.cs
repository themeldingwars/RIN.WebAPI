using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using FauFau.Net.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using RIN.WebAPI.Models;
using RIN.WebAPI.Models.User;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Controllers
{
    public class TmwController : Controller
    {
        private const string RED5_SIG1_NAME            = "X-Red5-Signature";
        private const string RED5_SIG2_NAME            = "X-Red5-Signature2";
        private const string RED5_HOT_FIX_LEVEL_NAME   = "X-Red5-Hotfix-Level";
        private const string RED5_SILO_POOL_ID_NAME    = "X-Red5-SiloPoolId";
        private const string RED5_IS_CENSORED_NAME     = "X-Red5-Is-Censored";
        private const string STEAM_USER_ID_NAME        = "X-Steam-Client-UserId";
        private const string STEAM_SESSION_TICKET_NAME = "X-Steam-Session-Ticket";

        protected readonly SessionManager SessionManager;

        public TmwController(SessionManager sessionManager)
        {
            SessionManager = sessionManager;
        }

        protected string GetSingleHeader(string headerName)
        {
            var str = Request.Headers.TryGetValue(headerName, out StringValues header)
                ? header.FirstOrDefault()
                : null;
            return str;
        }

        protected string           GetRed5Sig1Str()        => GetSingleHeader(RED5_SIG1_NAME);
        protected string           GetRed5Sig2Str()        => GetSingleHeader(RED5_SIG2_NAME);
        protected string           GetRed5HotfixLevel()    => GetSingleHeader(RED5_HOT_FIX_LEVEL_NAME);
        protected string           GetRed5SiloPollId()     => GetSingleHeader(RED5_SILO_POOL_ID_NAME);
        protected string           GetRed5Censored()       => GetSingleHeader(RED5_IS_CENSORED_NAME);
        protected Red5Sig.QsValues GetRed5Sig()            => Red5Sig.ParseString(GetRed5Sig1Str());
        protected string           GetSteamUserId()        => GetSingleHeader(STEAM_USER_ID_NAME);
        protected string           GetSteamSessionTicket() => GetSingleHeader(STEAM_SESSION_TICKET_NAME);

        protected string GetHeadersDev()
        {
            var sb = new StringBuilder();
            foreach (var kvp in Request.Headers) {
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }

            return sb.ToString();
        }

        protected string GetUid() => HttpUtility.UrlDecode(GetRed5Sig().UID.ToString());

        protected UserSessionData GetUserSession()
        {
            if (HttpContext.Items.TryGetValue(SessionManager.SESSION_KEY_PREFIX, out object sessionDataObj) && sessionDataObj is UserSessionData userSessionData)
                return userSessionData;

            var logger = HttpContext.RequestServices.GetService<ILogger<TmwController>>();
            logger.LogWarning("Failed to get session data for {uid}", GetUid());
            throw new TmwException(Error.Codes.ERR_UNKNOWN);

            /*
            var sessionManager            = HttpContext.RequestServices.GetService<SessionManager>();
            var (error, sessionData, uid) = sessionManager.GetOrCreateAtuthUserSession(HttpContext.Request);

            if (error != null)
            {
                var logger = HttpContext.RequestServices.GetService<ILogger<TmwController>>();
                logger.LogWarning("Failed to get session data for {uid}", uid);
                throw new TmwException(Error.Codes.ERR_UNKNOWN);
            }

            return sessionData;*/
        }

        protected void UpdateUserSession(Action<UserSessionData> updateFunc)
        {
            var sessionData = GetUserSession();
            if (sessionData != null)
            {
                var sessionManager = HttpContext.RequestServices.GetService<SessionManager>();

                sessionData.LastActiveTime = DateTime.UtcNow;
                updateFunc(sessionData);

                sessionManager.SetUserSessionData(GetUid(), sessionData);
            }
        }

        protected ContentResult ReturnError(string errorCode, string errorMessage, int statusCode = 500)
        {
            var json = JsonConvert.SerializeObject(new Error
            {
                code    = errorCode,
                message = errorMessage
            });

            var content = Content(json, "application/json");
            content.StatusCode = statusCode;
            return content;
        }

        protected ContentResult ReturnError(Error error, int statusCode = 500)
        {
            var json = JsonConvert.SerializeObject(error);

            var content = Content(json, "application/json");
            content.StatusCode = statusCode;
            return content;
        }
    }
}