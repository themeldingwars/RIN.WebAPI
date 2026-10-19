using System.Security.Cryptography;
using System.Text;

namespace RIN.Core.Utils
{
    public static class LoginTicket
    {
        public const string PREFIX        = "R5 ";
        public const int    SECRET_LENGTH = 32;
        public const int    HASH_LENGTH   = 32; // SHA-256

        private static readonly byte[] PrefixBytes = Encoding.ASCII.GetBytes(PREFIX);

        public static byte[] GenerateSecret()
        {
            var secret = new byte[SECRET_LENGTH];
            RandomNumberGenerator.Fill(secret);

            return secret;
        }

        /// <summary>
        /// Turns stored raw bytes into the base64 string the client API puts in the oracle/ticket response.
        /// The client then hands the game server the decoded payload, header included, which is what
        /// <see cref="TryDecodePayload"/> takes.
        /// </summary>
        public static string Encode(byte[] secret)
        {
            if (secret == null || secret.Length != SECRET_LENGTH)
                throw new ArgumentException($"A login ticket secret is {SECRET_LENGTH} bytes", nameof(secret));

            var payload = new byte[PrefixBytes.Length + secret.Length];
            PrefixBytes.CopyTo(payload, 0);
            secret.CopyTo(payload, PrefixBytes.Length);

            return Convert.ToBase64String(payload);
        }

        /// <summary>
        /// Strips the header from a presented ticket payload and returns the secret, or null when the
        /// payload is not the expected shape. Returns rather than throws: the bytes arrive over the network
        /// and can be anything.
        /// </summary>
        public static byte[] TryDecodePayload(byte[] payload)
        {
            if (payload == null || payload.Length != PrefixBytes.Length + SECRET_LENGTH)
                return null;

            if (!payload.AsSpan(0, PrefixBytes.Length).SequenceEqual(PrefixBytes))
                return null;

            return payload.AsSpan(PrefixBytes.Length, SECRET_LENGTH).ToArray();
        }

        /// <summary>
        /// The payload bytes behind a base64 ticket string - what the client hands the game server. Nothing in
        /// the login path needs this (the game server sends the payload directly); it is for support tooling
        /// and tests that start from a ticket copied out of a log.
        /// </summary>
        public static byte[] PayloadFromBase64(string ticket)
        {
            if (string.IsNullOrWhiteSpace(ticket))
                return null;

            try {
                return Convert.FromBase64String(ticket.Trim());
            }
            catch (FormatException) {
                return null;
            }
        }

        /// <summary>
        /// The lookup key. A plain SHA-256 is enough here because the ticket is 256 bits of CSPRNG output -
        /// unlike the password hash there is nothing to brute force.
        /// </summary>
        public static byte[] Hash(byte[] secret)
        {
            if (secret == null || secret.Length != SECRET_LENGTH)
                throw new ArgumentException($"A login ticket secret is {SECRET_LENGTH} bytes", nameof(secret));

            return SHA256.HashData(secret);
        }

        /// <summary>
        /// Constant-time compare of a presented secret against the stored one. The hash is only the lookup
        /// key; this is the actual check, so it compares the raw bytes.
        /// </summary>
        public static bool SecretsMatch(byte[] presented, byte[] stored)
        {
            if (presented == null || presented.Length != SECRET_LENGTH)
                return false;

            if (stored == null || stored.Length != SECRET_LENGTH)
                return false;

            return CryptographicOperations.FixedTimeEquals(presented, stored);
        }
    }
}
