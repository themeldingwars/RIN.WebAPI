using FauFau.Net.Web;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using ProtoBuf;
using RIN.WebAPI.Controllers;
using RIN.WebAPI.Models;
using RIN.WebAPI.Models.Config;
using RIN.WebAPI.Models.User;
using System.Reflection.Metadata;
using System.Web;

namespace RIN.WebAPI.Utils
{
    // Manage getting and updating a users session state
    public class SessionManager
    {
        private readonly ILogger<SessionManager> Logger;
        private readonly DB.DB Db;
        private readonly WebApiConfigSettings WebConfig;
        private readonly IDistributedCache Cache;

        public const string SESSION_KEY_PREFIX       = "USER_SESSION";
        private string GetUserSessionKey(string uuid) => $"{SESSION_KEY_PREFIX}:{uuid}";

        public SessionManager(ILogger<SessionManager> logger, DB.DB db, IOptions<WebApiConfigSettings> webConfig, IDistributedCache cache)
        {
            Logger    = logger;
            Db        = db;
            WebConfig = webConfig.Value;
            Cache     = cache;
        }

        private UserSessionData? GetUserSession(string uuid)
        {
            var key         = GetUserSessionKey(uuid);
            var data        = Cache.Get(key);
            if (data == null)
                return null;

            var sessionData = Serializer.Deserialize<UserSessionData>(data.AsSpan());
            return sessionData;
        }

        public (string? errorCode, UserSessionData? sessionData, string? uid) GetOrCreateAtuthUserSession(HttpRequest req)
        {
            var str = req.Headers.TryGetValue("X-Red5-Signature", out StringValues header) ? header.FirstOrDefault() : null;
            if (str == null)
            {
                Logger.LogWarning("Can't get or create session for request with no X-Red5-Signature. {Req}", req);
                return (Error.Codes.ERR_INCORRECT_USERPASS, null, null);
            }

            var r5Sig = Red5Sig.ParseString(str);
            var uid   = HttpUtility.UrlDecode(r5Sig.UID.ToString());

            var session = GetUserSession(uid);
            if (session == null)
            {
                var authDataTask = Db.GetAuthData(uid);
                authDataTask.Wait();
                var authData = authDataTask.Result;

                if (authData == null)
                {
                    Logger.LogWarning("Tried to get auth data for {UID} but no user was found with that UID. {Req}", uid, req);
                    return (Error.Codes.ERR_INCORRECT_USERPASS, null, uid);
                }

                var authed = Auth.Verify(authData.secret, str.AsSpan());
                if (authed)
                {
                    var sessionData = new UserSessionData()
                    {
                        AccountId = authData.account_id,
                        IsDev     = authData.is_dev,
                        Secert    = authData.secret
                    };

                    SetUserSessionData(uid, sessionData);
                    Logger.LogDebug("Created session for {UID}", uid);

                    return (null, sessionData, uid);
                }
            }
            else
            {
                var authed = Auth.Verify(session.Secert, str.AsSpan());
                if (authed)
                    return (null, session, uid); // Was in session, and auth matched, return session
                else // was in session but auth failed
                {
                    Logger.LogWarning("Auth failed for user {uid}, r5sig: {r5sig}, req: {Req}", uid, str, req);
                    return (Error.Codes.ERR_INCORRECT_USERPASS, null, uid); ;
                }
            }

            return (Error.Codes.ERR_INCORRECT_USERPASS, null, null); ;

            /*
            var loginResult = await Db.GetLoginData(uid);
            if (loginResult == null)
                context.Result = CreateError(Error.Codes.ERR_INCORRECT_USERPASS);

            var authed = Auth.Verify(loginResult.secret, str.AsSpan());
            if (!authed)
                context.Result = CreateError(Error.Codes.ERR_INCORRECT_USERPASS);
            */
        }

        public void UpdateUserSession(HttpRequest req, Action<UserSessionData> updateFunc)
        {
            var (errorCode, sessionData, uid) = GetOrCreateAtuthUserSession(req);
            if (errorCode != null)
            {
                Logger.LogWarning("Unable to update session data for {uid}", uid ?? "unknown");
                return;
            }

            sessionData.LastActiveTime = DateTime.UtcNow;
            updateFunc(sessionData);

            SetUserSessionData(uid, sessionData);
        }

        public void SetUserSessionData(string uid, UserSessionData sessionData)
        {
            var key  = GetUserSessionKey(uid);
            var data = Utils.ToProtoBuffByteArray(sessionData);

            Cache.Set(key, data, new DistributedCacheEntryOptions()
            {
                SlidingExpiration = WebConfig.UserSessionLifetime
            });
        }
    }
}
