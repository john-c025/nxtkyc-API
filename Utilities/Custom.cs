using System.Security.Cryptography;

namespace KYCAPI.Utilities
{
    public class Custom
    {
        private static readonly Random _random = new Random();

        public static string GenerateRandomPassword(int length = 7)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        public string GenerateRandomAccountNumber()
        {
            const string chars = "0123456789";
            var random = new Random();
            var result = new char[10];

            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] randomBytes = new byte[10];
                rng.GetBytes(randomBytes);

                for (int i = 0; i < 10; i++)
                {
                    result[i] = chars[randomBytes[i] % chars.Length];
                }
            }

            return "B" + new string(result); // Adds "B" prefix to the 10-digit number
        }
    }
}
