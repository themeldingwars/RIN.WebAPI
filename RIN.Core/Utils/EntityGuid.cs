namespace RIN.Core.Utils
{
    // From: https://gist.github.com/SilentCLD/881839a9f45578f1618db012fc789a71

    // Mostly speculation:
    // There are three types of GUIDs, starting with:
    // 1. Anything that isn't listed below (0x20, 0x1E, 0x0A, 0x46 etc)
    // 2. 0x7F
    // 3. 0xFF
    //
    // The first are v2 Guids, they appeared some time before 03/11/14 and conform to the structure below.
    //
    // The second are v1 Guids, they were the original implementation* and the structure is currently unknown.
    //   It's possible they were converted from another system and given the 0x7F range to not conflict with v2
    //
    // The third are also v2 Guids but are used for map entities which only need a unique identifier for the current map session/shard (NPCs, Deployables, Map markers etc).
    //   Map Guids will always start with 0xFF and will _not_ have the timestamp bits set

    //
    // Guid V2 Structure - Variation of MySQL UUID_SHORT()
    // ServerId  = Region + sql node cluster id? (1 byte)
    // Timestamp = Server boot time in Unix seconds shifted to the right by 8 bits (3 bytes)
    // Counter   = Auto increment counter shifted to the left by 8 bits (3 bytes)
    // Type      = Entity type id (1 byte)

    //
    // A Guid can be split in to two sections, High and Low
    // Using the following player Guid as an example: 0x1F54FA9E_38037701
    // The High part signifies where and roughly when a Guid was created - This can be initialized on startup of the application lifetime
    // The Low part signifies the partial ID and Type of entity - This must be created for each entity
    // If stored this way it may simplify full Guid creation
    // ulong full = ((ulong)hi << 32) | ((ulong)lo);

    //
    // Notes:
    // The Counter can be stored in memory - it doesn't need to persist between reboots as we also rely on Timestamp for uniqueness
    //
    // The Counter has an auto increment value of 100, this isn't strictly meaningful but may have been used to more easily create the Low part
    //
    // When 32bit Unix overflows in 2038 this should still continue working for another ~68 years as it will wrap around
    //
    // It is possible for a collision to occur if the server starts, generates a Guid, restarts then generates another straight away
    //   as we lose 256 seconds (4 minutes) of precision with the Timestamp shift.
    //   It would be best to use the start time of the system rather than the application.
    //
    // Account/Mail ids are distributed as a uint32 (3 byte Counter, 1 byte Type)

    //
    // Examples of Guids
    //
    // 0x7F3CB864AB349201 - v1 Player
    // 0x7FB962A3946C4101 - v1 Player
    // 0x205456D420319F01 - v2 Player
    // 0x1F54FA9E38037701 - v2 Player
    // (Types missing)
    // 0xFF000000000F0100 - v2 Map POI
    // 0xFF00000000009600 - v2 Health pickup
    // 0xFF00000000009A00 - v2 New You
    // 0x0B56AA25E40391FD - v2 Item
    // 0x0A56A7B5DC2C5AFD - v2 Item


    //
    // Example of parsing and generating a Guid
    //
    /*
    EntityGuid parsed = EntityGuid.Parse(0x1F54FA9E38037701);
    EntityGuid generated = new(31, 1425710592, 3670903, 0x01);

    Console.WriteLine( "        ServerId   Counter    Type           Timestamp                     Full");
    Console.WriteLine($"Parsed   : {parsed.ServerId}      {parsed.Counter}    0x{parsed.Type:X2}    {DateTimeOffset.FromUnixTimeSeconds(parsed.Timestamp)}    0x{parsed.Full:X16}");
    Console.WriteLine($"Generated: {generated.ServerId}      {generated.Counter}    0x{generated.Type:X2}    {DateTimeOffset.FromUnixTimeSeconds(generated.Timestamp)}    0x{generated.Full:X16}");
    */

    // Output:
    //
    //         ServerId   Counter    Type           Timestamp                     Full
    // Parsed   : 31      3670903    0x01    07/03/2015 06:43:12 +00:00    0x1F54FA9E38037701
    // Generated: 31      3670903    0x01    07/03/2015 06:43:12 +00:00    0x1F54FA9E38037701

    public readonly struct EntityGuid
    {
        public ulong Full => (ulong)((ulong)ServerId << 56) +
                             ((ulong)((Timestamp >> 8) & 0x00FFFFFF) << 32) +
                             (Counter << 8) +
                             Type;

        public byte ServerId { get; private init; }
        public uint Timestamp { get; private init; }
        public uint Counter { get; private init; }
        public byte Type { get; private init; }

        public EntityGuid(byte serverId, uint timestamp, uint counter, byte type)
        {
            ServerId = serverId;
            Timestamp = timestamp;
            Counter = counter;
            Type = type;
        }

        public static EntityGuid Parse(ulong guid)
        {
            EntityGuid entityGuid = new()
            {
                ServerId = (byte)(guid >> 56),
                Timestamp = (uint)(((guid >> 32) & 0x00FFFFFF) << 8),
                Counter = (uint)((guid & 0xFFFFFF00) >> 8),
                Type = (byte)(guid & 0xFF),
            };

            return entityGuid;
        }
    }

}
