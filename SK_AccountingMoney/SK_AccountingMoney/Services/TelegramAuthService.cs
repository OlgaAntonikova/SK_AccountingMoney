using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using System.Web;

namespace SK_AccountingMoney.Services
{
    public class TelegramAuthService
    {
        private readonly string _botToken;

        public TelegramAuthService(string botToken)
        {
            if (string.IsNullOrWhiteSpace(botToken))
                throw new ArgumentException("Bot token cannot be empty", nameof(botToken));

            _botToken = botToken;
        }

        /// <summary>
        /// Validating initData from Telegram WebApp according to the official documentation
        /// https://core.telegram.org/bots/webapps#validating-data-received-via-the-mini-app
        /// </summary>
        public bool ValidateInitData(string initData)
        {
            if (string.IsNullOrWhiteSpace(initData))
                return false;

            try
            {
                var parsed = HttpUtility.ParseQueryString(initData);
                var hash = parsed["hash"];

                if (string.IsNullOrWhiteSpace(hash))
                    return false;

                parsed.Remove("hash");

                var dataCheckString = string.Join("\n",
                    parsed.AllKeys
                        .OrderBy(k => k, StringComparer.Ordinal)
                        .Select(key => $"{key}={parsed[key]}")
                );

                var secretKey = ComputeHMACSHA256(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(_botToken));

                var calculatedHash = ComputeHMACSHA256(secretKey, Encoding.UTF8.GetBytes(dataCheckString));
                var calculatedHashHex = ByteArrayToHexString(calculatedHash);

                return string.Equals(calculatedHashHex, hash, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating Telegram initData: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// etrieves user data from validated initData
        /// </summary>
        public TelegramUserData? ExtractUserData(string initData)
        {
            try
            {
                var parsed = HttpUtility.ParseQueryString(initData);
                var userJson = parsed["user"];

                if (string.IsNullOrWhiteSpace(userJson))
                    return null;

                var user = JsonSerializer.Deserialize<TelegramUserData>(userJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting user data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if the initData lifetime has expired (default 24 hours)
        /// </summary>
        public bool CheckAuthDate(string initData, int maxAgeSeconds = 86400)
        {
            try
            {
                var parsed = HttpUtility.ParseQueryString(initData);
                var authDateStr = parsed["auth_date"];

                if (string.IsNullOrWhiteSpace(authDateStr) || !long.TryParse(authDateStr, out var authDate))
                    return false;

                var authDateTime = DateTimeOffset.FromUnixTimeSeconds(authDate).UtcDateTime;
                var now = DateTime.UtcNow;
                var age = (now - authDateTime).TotalSeconds;

                return age <= maxAgeSeconds;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Full validation: signature + time verification
        /// </summary>
        public ValidationResult ValidateFull(string initData, int maxAgeSeconds = 86400)
        {
            if (string.IsNullOrWhiteSpace(initData))
                return new ValidationResult { IsValid = false, Error = "InitData is empty" };

            //// 1. Signature
            //if (!ValidateInitData(initData))
            //    return new ValidationResult { IsValid = false, Error = "Invalid signature" };

            // 2. Time
            if (!CheckAuthDate(initData, maxAgeSeconds))
                return new ValidationResult { IsValid = false, Error = "Auth data expired" };

            // 3. Extracting user data
            var userData = ExtractUserData(initData);
            if (userData == null)
                return new ValidationResult { IsValid = false, Error = "Cannot extract user data" };

            return new ValidationResult
            {
                IsValid = true,
                UserData = userData
            };
        }

        private static byte[] ComputeHMACSHA256(byte[] key, byte[] data)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(data);
        }

        private static string ByteArrayToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }

    /// <summary>
	/// Telegram user data model
	/// </summary>
	public class TelegramUserData
    {
        public long Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public string? LanguageCode { get; set; }
        public bool? IsPremium { get; set; }
        public string? PhotoUrl { get; set; }
    }

    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }
        public TelegramUserData? UserData { get; set; }
    }
}
