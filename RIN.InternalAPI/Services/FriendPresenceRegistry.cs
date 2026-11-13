using System.Collections.Concurrent;
using RIN.InternalAPI.Models;

namespace RIN.InternalAPI.Services
{
    /// <summary>
    /// The characters that are online on any game server, as the game servers report them. Each entry belongs to the
    /// stream of the game server that reported it, so one that goes away can be cleaned up, and a late "offline" from
    /// the server a character just left doesn't undo the "online" from the server it moved to.
    /// </summary>
    public class FriendPresenceRegistry
    {
        private readonly ConcurrentDictionary<ulong, Entry> Online = new();

        // Characters that went offline recently, whose logout isn't announced yet in case they are only moving zones
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> Leaving = new();

        public bool TryGet(ulong characterGuid, out FriendPresence presence)
        {
            var found = Online.TryGetValue(characterGuid, out var entry);
            presence = entry?.Presence!;
            return found;
        }

        public bool IsOnline(ulong characterGuid) => Online.ContainsKey(characterGuid);

        /// <summary>
        /// Keeps the presence a game server reported. True when the character wasn't online before.
        /// </summary>
        public bool Set(ulong characterGuid, string name, FriendPresence presence, Guid owner)
        {
            var cameOnline = false;
            Online.AddOrUpdate(characterGuid,
                               _ => { cameOnline = true; return new Entry(name, presence, owner); },
                               (_, _) => { cameOnline = false; return new Entry(name, presence, owner); });
            return cameOnline;
        }

        /// <summary>
        /// Starts the wait before a logout is announced, the token is cancelled when the character comes back first
        /// </summary>
        public CancellationTokenSource BeginLeaving(ulong characterGuid)
        {
            var cts = new CancellationTokenSource();
            Leaving.AddOrUpdate(characterGuid, cts, (_, old) => { old.Cancel(); return cts; });
            return cts;
        }

        /// <summary>
        /// Called when a character comes online. True when it had only just left, so neither is worth announcing.
        /// </summary>
        public bool CancelLeaving(ulong characterGuid)
        {
            if (!Leaving.TryRemove(characterGuid, out var cts))
                return false;

            cts.Cancel();
            return true;
        }

        /// <summary>
        /// Called when the wait is over. True when this wait is still the current one, and the logout is to be announced.
        /// </summary>
        public bool EndLeaving(ulong characterGuid, CancellationTokenSource cts)
        {
            return Leaving.TryRemove(new KeyValuePair<ulong, CancellationTokenSource>(characterGuid, cts));
        }

        /// <summary>
        /// Takes a character offline, unless another game server has reported it since
        /// </summary>
        public bool Remove(ulong characterGuid, Guid owner)
        {
            return Online.TryGetValue(characterGuid, out var entry)
                && entry.Owner == owner
                && Online.TryRemove(new KeyValuePair<ulong, Entry>(characterGuid, entry));
        }

        /// <summary>
        /// Takes every character a game server reported offline, for when its stream ended
        /// </summary>
        public List<(ulong characterGuid, string name)> RemoveOwnedBy(Guid owner)
        {
            var removed = new List<(ulong, string)>();
            foreach (var (characterGuid, entry) in Online)
            {
                if (entry.Owner == owner && Online.TryRemove(new KeyValuePair<ulong, Entry>(characterGuid, entry)))
                    removed.Add((characterGuid, entry.Name));
            }

            return removed;
        }

        private record Entry(string Name, FriendPresence Presence, Guid Owner);
    }
}
