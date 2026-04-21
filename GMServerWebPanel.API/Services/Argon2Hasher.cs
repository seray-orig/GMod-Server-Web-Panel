using GMServerWebPanel.API.Services.Interfaces;
using GMServerWebPanel.API.Settings;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace GMServerWebPanel.API.Services
{
    public class Argon2Hasher(IOptions<Argon2Settings> settings) : IPasswordHasher
    {
        private readonly Argon2Settings _settings = settings.Value;

        public string HashPassword(string password)
        {
            byte[] salt = new byte[_settings.SaltLength];
            RandomNumberGenerator.Fill(salt);

            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = _settings.DegreeOfParallelism,
                Iterations = _settings.Iterations,
                MemorySize = _settings.MemorySize
            };

            byte[] hash = argon2.GetBytes(_settings.HashLength);

            // Символ доллара - криптографический разделитель частей.
            return
                $"argon2id" +
                $"$m={_settings.MemorySize},t={_settings.Iterations},p={_settings.DegreeOfParallelism}" +
                $"${Convert.ToBase64String(salt)}" +
                $"${Convert.ToBase64String(hash)}";
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            var parts = passwordHash.Split('$');
            if (parts.Length != 4) return false;

            var settingsRaw = parts[1].Split(',');

            if (!Int32.TryParse(settingsRaw[0].Replace("m=", ""), out int m) ||
                !Int32.TryParse(settingsRaw[1].Replace("t=", ""), out int t) ||
                !Int32.TryParse(settingsRaw[2].Replace("p=", ""), out int p) ||
                m < 4 || t < 1 || p < 1)
                return false;

            byte[] salt;
            byte[] hashToCompare;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                hashToCompare = Convert.FromBase64String(parts[3]);
            }
            catch
            {
                return false;
            }

            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = p,
                Iterations = t,
                MemorySize = m
            };

            return (CryptographicOperations.FixedTimeEquals(argon2.GetBytes(hashToCompare.ToArray().Length), hashToCompare));
        }
    }
}
