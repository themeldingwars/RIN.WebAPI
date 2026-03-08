using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Npgsql;
using RIN.Core.DB;
using RIN.InternalAPI.Models;

namespace RIN.InternalAPI.Services
{
    public class DbEventBus : BackgroundService
    {
        private readonly string ConnStr;
        private readonly DB Db;
        private readonly ILogger<DbEventBus> Logger;
        private readonly ConcurrentDictionary<Guid, Channel<Event>> Subscribers = new();
        private readonly Channel<Event> InternalChannel = Channel.CreateUnbounded<Event>();
        private static readonly ConcurrentDictionary<string, Type?> EventTypeCache = new();

        public DbEventBus(DB db, ILogger<DbEventBus> logger)
        {
            Db = db;
            ConnStr = db.ConnStr;
            Logger = logger;
        }

        public Guid Subscribe(Channel<Event> channel)
        {
            var id = Guid.NewGuid();
            Subscribers.TryAdd(id, channel);
            return id;
        }

        public void Unsubscribe(Guid id) => Subscribers.TryRemove(id, out _);

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var listenTask = ListenToPostgres(cts.Token);
                var processTask = ProcessEventsAsync(cts.Token);

                await Task.WhenAny(listenTask, processTask);
                await cts.CancelAsync();
                await Task.WhenAll(listenTask, processTask);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                Logger.LogError(ex, ex.Message);
            }
        }

        private async Task ListenToPostgres(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await using var conn = new NpgsqlConnection(ConnStr);
                    await conn.OpenAsync(ct);
                    conn.Notification += (_, e) => ParseAndQueueEvent(e.Payload);

                    await using (var cmd = new NpgsqlCommand("LISTEN events", conn))
                    {
                        await cmd.ExecuteNonQueryAsync(ct);
                    }

                    Logger.LogInformation("Listening for db events");

                    while (!ct.IsCancellationRequested) 
                    {
                        await conn.WaitAsync(ct);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Logger.LogError(ex, "DbEventBus error ocurred, reconnecting in 10 seconds");
                    await Task.Delay(10000, ct);
                }
            }
        }

        private void ParseAndQueueEvent(string payloadJson)
        {
            try
            {
                var dbEvent = payloadJson.Split(["->"], StringSplitOptions.None);
                if (dbEvent.Length != 2) return;
                
                var eventType = dbEvent[0];
                var payloadStr = dbEvent[1];

                var type = EventTypeCache.GetOrAdd(eventType, type => Type.GetType($"RIN.InternalAPI.Models.{type}"));
                if (type == null)
                {
                    Logger.LogWarning("Unknown event type: {eventType}", eventType);
                    return;
                }

                var payload = JsonSerializer.Deserialize(payloadStr, type);
                if (payload is Event evt)
                {
                    InternalChannel.Writer.TryWrite(evt);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error parsing event payload: {payloadJson}", payloadJson);
            }
        }

        private async Task ProcessEventsAsync(CancellationToken ct)
        {
            await foreach (var evt in InternalChannel.Reader.ReadAllAsync(ct))
            {
                try
                {
                    if (evt is CharacterVisualsUpdated cvu)
                    {
                        _ = FetchCharacterVisuals(cvu);
                    }
                    else
                    {
                        DispatchToAll(evt);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error processing event");
                }
            }
        }

        private async Task FetchCharacterVisuals(CharacterVisualsUpdated cvu)
        {
            try
            {
                var result = await Db.GetBasicCharacterAndVisualData((long)cvu.CharacterGuid);
                var bfVisuals = PlayerBattleframeVisuals.CreateDefault();

                cvu.CharacterAndBattleframeVisuals = new CharacterAndBattleframeVisuals
                {
                    CharacterInfo = result.info,
                    CharacterVisuals = result.visuals,
                    BattleframeVisuals = bfVisuals
                };

                DispatchToAll(cvu);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while fetching character and battleframe visuals: {message}", ex.Message);
            }
        }

        private void DispatchToAll(Event evt)
        {
            foreach (var sub in Subscribers)
            {
                sub.Value.Writer.TryWrite(evt);
            }
        }
    }
}
