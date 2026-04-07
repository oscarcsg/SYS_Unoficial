using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace StoreYourStuffAPI.Security
{
    public class Argon2PasswordHasher : IPasswordHasher
    {
        #region Configuration
        private const int DegreeOfParallelism = 4; // CPU cores to use
        private const int Iterations = 3;          // Proccessing time (higher = slower)
        private const int MemorySize = 65536;      // RAM memory required in KB (64 MB)
        private const int SaltSize = 16;           // Salt size (16 bytes)
        private const int HashSize = 32;           // Hash final size (32 bytes)
        #endregion

        #region Implementations
        public string HashPassword(string password)
        {
            byte[] salt = CreateSalt();
            byte[] hash = GenerateHash(password, salt);

            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 2) return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expectedHash = Convert.FromBase64String(parts[1]);

            byte[] actualHash = GenerateHash(password, salt);

            // Prevents timing attacks
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        #endregion

        #region Methods
        private static byte[] CreateSalt()
        {
            var buffer = new byte[SaltSize];
            RandomNumberGenerator.Fill(buffer);
            return buffer;
        }

        private static byte[] GenerateHash(string password, byte[] salt)
        {
            var bytes = Encoding.UTF8.GetBytes(password);

            using var argon2 = new Argon2id(bytes)
            {
                Salt = salt,
                DegreeOfParallelism = DegreeOfParallelism,
                Iterations = Iterations,
                MemorySize = MemorySize
            };

            return argon2.GetBytes(HashSize);
        }
        #endregion
    }
}
