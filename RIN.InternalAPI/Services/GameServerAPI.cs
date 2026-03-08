using System.Threading.Channels;
using Grpc.Core;
using RIN.Core.DB;
using RIN.InternalAPI.Models;

namespace RIN.InternalAPI.Services
{
    public class GameServerAPI : IGameServerAPI
    {
        private readonly DB Db;
        private readonly ILogger<GameServerAPI> Logger;
        private readonly DbEventBus EventBus;
        private readonly IHostApplicationLifetime Lifetime;

        public GameServerAPI(DB db, ILogger<GameServerAPI> logger, DbEventBus eventBus, IHostApplicationLifetime lifetime)
        {
            Db     = db;
            Logger = logger;
            EventBus = eventBus;
            Lifetime = lifetime;
        }

        public async ValueTask<PingResp> Ping(PingReq req)
        {
            var resp = new PingResp
            {
                ClientSentTime   = req.SentTime,
                ServerReciveTime = DateTime.UtcNow
            };

            return resp;
        }

        public async ValueTask<CharacterAndBattleframeVisuals> GetCharacterAndBattleframeVisuals(CharacterID req)
        {
            var result    = await Db.GetBasicCharacterAndVisualData(req.ID);
            var bfVisuals = PlayerBattleframeVisuals.CreateDefault();

            var resp = new CharacterAndBattleframeVisuals
            {
                CharacterInfo      = result.info,
                CharacterVisuals   = result.visuals,
                BattleframeVisuals = bfVisuals
            };

            return resp;
        }

        public async Task Stream(IAsyncStreamReader<Command> commands, IServerStreamWriter<Event> events, ServerCallContext context)
        {
            var channel = Channel.CreateUnbounded<Event>();
            var subscriptionId = EventBus.Subscribe(channel);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, Lifetime.ApplicationStopping);
            var token = cts.Token;
            try
            {
                var sendEventsTask = Task.Run(async () =>
                {
                    await foreach (var evt in channel.Reader.ReadAllAsync(token))
                    {
                        await events.WriteAsync(evt);
                    }
                });

                var commandsTask = Task.Run(async () =>
                {
                    await foreach (var command in commands.ReadAllAsync(token))
                    {
                        Logger.LogInformation("Received command: {command}", command);

                        switch (command)
                        {
                            case SaveGameSessionData data:
                                await Db.UpdateCharacterAfterGameSession((long)data.CharacterId, (int)data.ZoneId, (int)data.OutpostId, (int)data.TimePlayed);
                                break;
                            case SaveLgvRaceFinish race:
                                await Db.SaveLgvRaceFinish((long)race.CharacterGuid, (int)race.LeaderboardId, (long)race.TimeMs);
                                break;
                        }
                    }
                });

                await Task.WhenAny(sendEventsTask, commandsTask);
                await cts.CancelAsync();
                await Task.WhenAll(sendEventsTask, commandsTask);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogError(ex, "GRPC Stream crashed");
            }
            finally
            {
                channel.Writer.TryComplete();
                EventBus.Unsubscribe(subscriptionId);
            }
        }
    }
}
