using RIN.InternalAPI.Models;
using System.Reflection.Metadata.Ecma335;

namespace RIN.InternalAPI.Services
{
    public class GameServerAPI : IGameServerAPI
    {
        public async ValueTask<PingResp> Ping(PingReq req)
        {
            var resp = new PingResp
            {
                ClientSentTime   = req.SentTime,
                ServerReciveTime = DateTime.UtcNow
            };

            return resp;
        }
    }
}
