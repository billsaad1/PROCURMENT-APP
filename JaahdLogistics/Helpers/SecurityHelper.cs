using System;
using System.Security.Cryptography;
using System.Text;

namespace JaahdLogistics.Helpers
{
    public static class SecurityHelper
    {
        private const string GlobalSalt = "JAAHD_SALT_2024_!"; // In production, use per-user random salts

        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var combined = password + GlobalSalt;
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                // Add a prefix to identify hashed passwords
                return "$SHA2$" + BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(hash)) return false;

            // Backward compatibility check for un-salted/un-prefixed hashes if any exist
            if (hash.Length == 64 && !hash.StartsWith("$"))
            {
                using (var sha256 = SHA256.Create())
                {
                    var oldHashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                    var oldHash = BitConverter.ToString(oldHashedBytes).Replace("-", "").ToLower();
                    if (oldHash == hash) return true;
                }
            }

            return HashPassword(password) == hash;
        }
    }
}
