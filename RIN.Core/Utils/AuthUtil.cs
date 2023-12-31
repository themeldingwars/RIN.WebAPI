using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace RIN.Core.Utils
{
    public static class AuthUtil
    {
        public const int PASSWORD_SALT_LENGTH     = 128 / 8;
        public const int PASSWORD_HASH_LENGTH     = 256 / 8;
        const        int PASSWORD_HASH_BUFFER_SIZE = PASSWORD_SALT_LENGTH + PASSWORD_HASH_LENGTH;
        const        int PASSWORD_ITERATION_COUNT = 10000;

        // Create a new salt and password hash for a users password
        public static byte[] CreatePasswordHashAndSalt(string password)
        {
            Span<byte> buffer = stackalloc byte[PASSWORD_HASH_BUFFER_SIZE];

            byte[] salt = new byte[PASSWORD_SALT_LENGTH];
            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(salt);
            }

            var hashed = CreatePasswordHash(password, salt);

            // Combine into one
            salt.AsSpan().CopyTo(buffer);
            hashed.AsSpan().CopyTo(buffer.Slice(PASSWORD_SALT_LENGTH));

            return buffer.ToArray();
        }

        public static bool VerifyPassword(string password, ReadOnlySpan<byte> hashedPassword)
        {
            var hasEmailAndPass = password.Length > 0 && !hashedPassword.IsEmpty;
            if (!hasEmailAndPass) {
                return false;
            }

            var salt       = hashedPassword.Slice(0, PASSWORD_SALT_LENGTH);
            var hashedPass = hashedPassword.Slice(PASSWORD_SALT_LENGTH, PASSWORD_HASH_LENGTH);

            ReadOnlySpan<byte> newHash = CreatePasswordHash(password, salt).AsSpan();
            var                matched = hashedPass.SequenceEqual(newHash);

            return matched;
        }

        private static byte[] CreatePasswordHash(string password, ReadOnlySpan<byte> salt)
        {
            var hashed = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt.ToArray(),
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: PASSWORD_ITERATION_COUNT,
                numBytesRequested: PASSWORD_HASH_LENGTH);

            return hashed;
        }
    }
}