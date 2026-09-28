using System.Threading.Channels;
using ProtoBuf.Grpc;
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

        public async IAsyncEnumerable<Event> Stream(IAsyncEnumerable<Command> commands, CallContext context = default)
        {
            var channel = Channel.CreateUnbounded<Event>();
            var subscriptionId = EventBus.Subscribe(channel);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, Lifetime.ApplicationStopping);
            var token = cts.Token;
            var commandsTask = ReadCommands(commands, channel, token);

            try
            {
                while (true)
                {
                    Event evt;
                    try
                    {
                        evt = await channel.Reader.ReadAsync(token);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException and not ChannelClosedException)
                        {
                            Logger.LogError(ex, "GRPC Stream crashed");
                        }

                        break;
                    }

                    yield return evt;
                }
            }
            finally
            {
                await cts.CancelAsync();
                channel.Writer.TryComplete();
                EventBus.Unsubscribe(subscriptionId);
                await commandsTask;
            }
        }

        private async Task ReadCommands(IAsyncEnumerable<Command> commands, Channel<Event> channel, CancellationToken token)
        {
            try
            {
                await foreach (var command in commands.WithCancellation(token))
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
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log here as well, as it runs in a separate task and won't be caught by the main loop
                Logger.LogError(ex, "GRPC Stream crashed");
            }
            finally
            {
                channel.Writer.TryComplete();
            }
        }
    }
}
