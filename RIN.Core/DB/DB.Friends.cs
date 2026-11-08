using Dapper;

namespace RIN.Core.DB;

public partial class DB
{
    /// <summary>
    /// The character's friends list. Characters that are deleted, or queued to be, stay off it, the same markers
    /// GetLoginCharacter checks.
    /// </summary>
    public async Task<IEnumerable<CharacterFriendRow>> GetCharacterFriends(long characterGuid)
    {
        const string SELECT_SQL = @"
                SELECT f.friend_guid,
                       c.name,
                       f.status,
                       f.note,
                       EXTRACT(EPOCH FROM c.last_seen_at)::bigint AS last_seen_at
                FROM webapi.""CharacterFriends"" f
                INNER JOIN webapi.""Characters"" c ON c.character_guid = f.friend_guid
                WHERE f.character_guid = @characterGuid
                AND c.deleted_at IS NULL
                AND NOT EXISTS (SELECT 1 FROM webapi.""DeletionQueue"" dq WHERE dq.character_guid = c.character_guid)
                ORDER BY c.name";

        return await DBCall(async conn => await conn.QueryAsync<CharacterFriendRow>(SELECT_SQL, new { characterGuid }))
            ?? Enumerable.Empty<CharacterFriendRow>();
    }

    /// <summary>
    /// Add a friend by name, matched without case. Not through unique_name, which older characters can have out of
    /// step with the name. A friendship starts as a request; adding someone who already asked accepts their request
    /// instead. Adding someone already asked or already a friend only replaces the note, and only when one is given.
    /// </summary>
    public async Task<(FriendUpdateStatus status, CharacterFriendRow friend)> AddCharacterFriend(long characterGuid, string friendName, string? note)
    {
        const string FIND_SQL = @"
                SELECT c.character_guid AS friend_guid,
                       c.name,
                       EXTRACT(EPOCH FROM c.last_seen_at)::bigint AS last_seen_at
                FROM webapi.""Characters"" c
                WHERE UPPER(c.name) = UPPER(@friendName)
                AND c.deleted_at IS NULL
                AND NOT EXISTS (SELECT 1 FROM webapi.""DeletionQueue"" dq WHERE dq.character_guid = c.character_guid)
                LIMIT 1";

        // Changes to a friendship are made one at a time, under a lock on the lower of the two guids. Two characters
        // adding each other at once then can't both send a request, the second accepts the first's.
        const string LOCK_SQL = @"SELECT pg_advisory_xact_lock(LEAST(@characterGuid, @friendGuid))";

        const string STATUS_SQL = @"
                SELECT status
                FROM webapi.""CharacterFriends""
                WHERE character_guid = @characterGuid
                AND friend_guid = @friendGuid";

        const string REQUEST_SQL = @"
                INSERT INTO webapi.""CharacterFriends"" (character_guid, friend_guid, status)
                VALUES (@characterGuid, @friendGuid, 2),
                       (@friendGuid, @characterGuid, 3)";

        const string ACCEPT_SQL = @"
                UPDATE webapi.""CharacterFriends""
                SET status = 1
                WHERE (character_guid, friend_guid) IN ((@characterGuid, @friendGuid), (@friendGuid, @characterGuid))";

        const string NOTE_SQL = @"
                UPDATE webapi.""CharacterFriends""
                SET note = COALESCE(@note, note)
                WHERE character_guid = @characterGuid
                AND friend_guid = @friendGuid
                RETURNING status, note";

        var failed = false;
        var result = await DBCall(async conn =>
        {
            await using var tx = await conn.BeginTransactionAsync();

            var friend = await conn.QueryFirstOrDefaultAsync<CharacterFriendRow>(FIND_SQL, new { friendName }, tx);
            if (friend == null)
                return (FriendUpdateStatus.NotFound, null!);

            if (friend.friend_guid == characterGuid)
                return (FriendUpdateStatus.Self, friend);

            var args = new { characterGuid, friendGuid = friend.friend_guid, note };
            await conn.ExecuteAsync(LOCK_SQL, args, tx);

            FriendUpdateStatus outcome;
            var status = await conn.QueryFirstOrDefaultAsync<short?>(STATUS_SQL, args, tx);
            if (status == null)
            {
                await conn.ExecuteAsync(REQUEST_SQL, args, tx);
                outcome = FriendUpdateStatus.Requested;
            }
            else if (status == CharacterFriendStatus.RequestReceived)
            {
                await conn.ExecuteAsync(ACCEPT_SQL, args, tx);
                outcome = FriendUpdateStatus.Accepted;
            }
            else
            {
                outcome = FriendUpdateStatus.Unchanged;
            }

            var entry = await conn.QuerySingleAsync<CharacterFriendRow>(NOTE_SQL, args, tx);
            friend.status = entry.status;
            friend.note   = entry.note;

            await tx.CommitAsync();
            return (outcome, friend);
        }, _ => failed = true);

        return failed ? (FriendUpdateStatus.Error, null!) : result;
    }

    /// <summary>
    /// Replace the note the character keeps on an entry of its list, whatever its status. The friend's own note is
    /// not touched, a note is only seen by the one who wrote it.
    /// </summary>
    public async Task<(FriendUpdateStatus status, CharacterFriendRow friend)> SetCharacterFriendNote(long characterGuid, string friendName, string note)
    {
        const string UPDATE_SQL = @"
                UPDATE webapi.""CharacterFriends"" f
                SET note = @note
                FROM webapi.""Characters"" c
                WHERE f.character_guid = @characterGuid
                AND c.character_guid = f.friend_guid
                AND UPPER(c.name) = UPPER(@friendName)
                RETURNING f.friend_guid,
                          c.name,
                          f.status,
                          f.note,
                          EXTRACT(EPOCH FROM c.last_seen_at)::bigint AS last_seen_at";

        var failed = false;
        var friend = await DBCall(
            conn => conn.QueryFirstOrDefaultAsync<CharacterFriendRow>(UPDATE_SQL, new { characterGuid, friendName, note }),
            _ => failed = true);

        if (failed)
            return (FriendUpdateStatus.Error, null!);

        return friend == null ? (FriendUpdateStatus.NotFound, null!) : (FriendUpdateStatus.Unchanged, friend);
    }

    /// <summary>
    /// Take a friend off a list by name, which takes the character off the friend's list too. The same call ends a
    /// friendship, takes back a request the character sent and declines one it received, which one depends on what
    /// was there. Deleted characters still match, so they can be removed.
    /// </summary>
    public async Task<(FriendUpdateStatus status, CharacterFriendRow friend)> RemoveCharacterFriend(long characterGuid, string friendName)
    {
        const string FIND_SQL = @"
                SELECT f.friend_guid,
                       c.name,
                       f.note,
                       EXTRACT(EPOCH FROM c.last_seen_at)::bigint AS last_seen_at
                FROM webapi.""CharacterFriends"" f
                INNER JOIN webapi.""Characters"" c ON c.character_guid = f.friend_guid
                WHERE f.character_guid = @characterGuid
                AND UPPER(c.name) = UPPER(@friendName)
                LIMIT 1";

        // The same lock AddCharacterFriend takes
        const string LOCK_SQL = @"SELECT pg_advisory_xact_lock(LEAST(@characterGuid, @friendGuid))";

        // The status as it was under the lock, a concurrent call can have changed or removed it since the find
        const string DELETE_SQL = @"
                WITH removed AS (
                    DELETE FROM webapi.""CharacterFriends""
                    WHERE (character_guid, friend_guid) IN ((@characterGuid, @friendGuid), (@friendGuid, @characterGuid))
                    RETURNING character_guid, status
                )
                SELECT status FROM removed WHERE character_guid = @characterGuid";

        var failed = false;
        var result = await DBCall(async conn =>
        {
            await using var tx = await conn.BeginTransactionAsync();

            var friend = await conn.QueryFirstOrDefaultAsync<CharacterFriendRow>(FIND_SQL, new { characterGuid, friendName }, tx);
            if (friend == null)
                return (FriendUpdateStatus.NotFound, null!);

            var args = new { characterGuid, friendGuid = friend.friend_guid };
            await conn.ExecuteAsync(LOCK_SQL, args, tx);

            var status = await conn.QueryFirstOrDefaultAsync<short?>(DELETE_SQL, args, tx);
            if (status == null)
                return (FriendUpdateStatus.NotFound, null!);

            await tx.CommitAsync();

            friend.status = status.Value;
            var outcome = status.Value switch
            {
                CharacterFriendStatus.RequestSent     => FriendUpdateStatus.Cancelled,
                CharacterFriendStatus.RequestReceived => FriendUpdateStatus.Declined,
                _                                     => FriendUpdateStatus.Removed
            };

            return (outcome, friend);
        }, _ => failed = true);

        return failed ? (FriendUpdateStatus.Error, null!) : result;
    }

    // The other members of the character's army
    public async Task<IEnumerable<long>> GetArmyMates(long characterGuid)
    {
        const string SELECT_SQL = @"
                SELECT mate.character_guid
                FROM webapi.""ArmyMembers"" self
                INNER JOIN webapi.""ArmyMembers"" mate ON mate.army_guid = self.army_guid
                WHERE self.character_guid = @characterGuid
                AND mate.character_guid <> @characterGuid";

        return await DBCall(async conn => await conn.QueryAsync<long>(SELECT_SQL, new { characterGuid }))
            ?? Enumerable.Empty<long>();
    }
}
