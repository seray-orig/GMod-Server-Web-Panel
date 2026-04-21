using GMServerWebPanel.API.Services;
using GMServerWebPanel.API.Settings;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace GMServerWebPanel.API.UnitTests
{
    public class Argon2HasherTests
    {
        private readonly IOptions<Argon2Settings> argon2Settings = Options.Create(new Argon2Settings()
        {
            // При нуле - стд настройки.
            MemorySize = 0,
            Iterations = 0,
            DegreeOfParallelism = 0,
        });

        [Fact]
        public void HashFormat_HasFourSegments()
        {
            // Arrange
            var passwordHasher = new Argon2Hasher(argon2Settings);

            var word = "Чикибамбони$";

            // Act
            var hash = passwordHasher.HashPassword(word);

            var parts = hash.Split('$');

            // Assert
            Assert.Equal(4, parts.Length);

            Assert.False(string.IsNullOrEmpty(parts[0]));
            Assert.StartsWith("argon2id", parts[0]);

            Assert.False(string.IsNullOrEmpty(parts[1]));

            Assert.False(string.IsNullOrEmpty(parts[2]));

            Assert.False(string.IsNullOrEmpty(parts[3]));
        }

        [Fact]
        public void HashFormat_Contains_Argon2Parameters()
        {
            // Arrange
            var passwordHasher = new Argon2Hasher(argon2Settings);

            var word = "Чикибамбони$";

            // Act
            var hash = passwordHasher.HashPassword(word);

            var parts = hash.Split('$');

            var settingsRaw = parts[1].Split(',');

            // Assert
            Assert.Equal(3, settingsRaw.Length);

            Assert.False(string.IsNullOrEmpty(settingsRaw[0]));
            Assert.StartsWith("m=", settingsRaw[0]);

            Assert.False(string.IsNullOrEmpty(settingsRaw[1]));
            Assert.StartsWith("t=", settingsRaw[1]);

            Assert.False(string.IsNullOrEmpty(settingsRaw[2]));
            Assert.StartsWith("p=", settingsRaw[2]);
        }

        [Fact]
        public void HashPasswords_HashTwoSameWords_ReturnsDifferentHashes_ButBothVerify()
        {
            // Arrange
            var passwordHasher = new Argon2Hasher(argon2Settings);

            var word = "Чикибамбони$";

            // Act
            var hashFirst = passwordHasher.HashPassword(word);
            var hashSecond = passwordHasher.HashPassword(word);

            var verifyFirst = passwordHasher.VerifyPassword(word, hashFirst);
            var verifySecond = passwordHasher.VerifyPassword(word, hashSecond);

            // Assert
            Assert.NotEqual(hashFirst, hashSecond);

            Assert.True(verifyFirst);
            Assert.True(verifySecond);
        }

        [Fact]
        public void VerifyPassword_WrongPassword_ReturnsFalse()
        {
            // Arrange
            var passwordHasher = new Argon2Hasher(argon2Settings);
            var correctPassword = "Чикибамбони$";
            var wrongPassword = "Чикибамбони$123";

            var hash = passwordHasher.HashPassword(correctPassword);

            // Act
            var result = passwordHasher.VerifyPassword(wrongPassword, hash);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_InvalidHashFormat_ReturnsFalse()
        {
            // Arrange
            var passwordHasher = new Argon2Hasher(argon2Settings);
            var password = "Чикибамбони$";

            var invalidHash = "invalid_hash_without_delimiters";

            // Act
            var result = passwordHasher.VerifyPassword(password, invalidHash);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_InvalidBase64_ThrowsException()
        {
            // Arrange
            var passwordHasher = new Argon2Hasher(argon2Settings);
            var password = "Чикибамбони$";

            var invalidHash = "argon2id$m=4,t=1,p=1$not_base64$also_not_base64";

            // Act
            var result = passwordHasher.VerifyPassword(password, invalidHash);

            // Assert
            Assert.False(result);
        }

        [Theory]
        // Выход за минимум.
        [InlineData(3, 1, 1)]
        [InlineData(4, 0, 1)]
        [InlineData(4, 1, 0)]
        // Другие типы данных.
        [InlineData(true, 1, 1)]
        [InlineData(4, false, 1)]
        [InlineData(4, 1, false)]
        [InlineData('r', 1, 1)]
        [InlineData(4, 'g', 1)]
        [InlineData(4, 1, 'b')]
        [InlineData("test", "false", "invalid")]
        public void ValidationArgon2Settings_ReturnsFalse_NotExeption(object memory, object iterations, object parallelism)
        {
            // Arrange
            var passwordHasher = new Argon2Hasher(argon2Settings);
            var password = "Чикибамбони$";

            var invalidHash = "argon2id" +
                $"$m={memory},t={iterations},p={parallelism}" +
                "$test" +
                "$test";

            // Act
            var result = passwordHasher.VerifyPassword(password, invalidHash);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_ModifiedParameters_ReturnsFalse()
        {
            // Arrange
            var argon2Settings = Options.Create(new Argon2Settings()
            {
                // При нуле - стд настройки.
                MemorySize = 128 * 1024,
                Iterations = 0,
                DegreeOfParallelism = 0,
            });

            var passwordHasher = new Argon2Hasher(argon2Settings);

            var password = "Чикибамбони$";

            var hash = passwordHasher.HashPassword(password);

            var parts = hash.Split('$');
            parts[1] = $"m={argon2Settings.Value.MemorySize * 2},t=1,p=1";

            var modifiedHash = string.Join("$", parts);

            // Act
            var result = passwordHasher.VerifyPassword(password, modifiedHash);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_WithDifferentHashParameters_Succeeds()
        {
            // Arrange
            var weakSettings = Options.Create(new Argon2Settings
            {
                MemorySize = 32 * 1024,
                Iterations = 1,
                DegreeOfParallelism = 1,
                HashLength = 16
            });

            var weakHasher = new Argon2Hasher(weakSettings);

            var password = "Чикибамбони$";

            var hash = weakHasher.HashPassword(password);

            var strongSettings = Options.Create(new Argon2Settings
            {
                MemorySize = 512 * 1024,
                Iterations = 100,
                DegreeOfParallelism = 8,
                HashLength = 64
            });
            var strongHasher = new Argon2Hasher(strongSettings);

            // Act & Assert
            Assert.True(strongHasher.VerifyPassword(password, hash));
        }
        
        /*
        public static IEnumerable<object[]> GetUniquePasswords(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new object[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            }
        }

        [Theory]
        [MemberData(nameof(GetUniquePasswords), 70)]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void VerifyPassword_ConstantTimeComparison(string password, string wrongPassword)
        {
            // Arrange
            var maxDiff = 5; // %

            var argon2Settings = Options.Create(new Argon2Settings
            {
                MemorySize = 512 * 1024,
                Iterations = 20,
                DegreeOfParallelism = 0,
            });

            var hasher = new Argon2Hasher(argon2Settings);

            var hash = hasher.HashPassword(password);

            // Act
            // Так называемый прогрев.
            hasher.VerifyPassword(password, hash);

            var startSucces = Stopwatch.GetTimestamp();
            hasher.VerifyPassword(password, hash);
            var endSucces = Stopwatch.GetTimestamp();

            var startWrong = Stopwatch.GetTimestamp();
            hasher.VerifyPassword(wrongPassword, hash);
            var endWrong = Stopwatch.GetTimestamp();

            var successTime = Stopwatch.GetElapsedTime(startSucces, endSucces).TotalMilliseconds;
            var wrongTime = Stopwatch.GetElapsedTime(startWrong, endWrong).TotalMilliseconds;
            var maxTime = Math.Max(successTime, wrongTime);
            var diffPercent = maxTime > 0 ? Math.Abs(successTime - wrongTime) / maxTime * 100 : 0;

            // Assert
            Assert.True(diffPercent <= maxDiff, $"Разница по времени {diffPercent:F2}% превысила порог {maxDiff}%");
        }
        */
    }
}
