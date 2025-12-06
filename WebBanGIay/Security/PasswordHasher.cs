using System;
using System.Security.Cryptography;
using System.Text;

namespace WebBanGIay.Security
{
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password không được để trống.");

            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public static bool Verify(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            var hashInput = Hash(password);
            return string.Equals(hashInput, hashedPassword, StringComparison.Ordinal);
        }
    }
}
