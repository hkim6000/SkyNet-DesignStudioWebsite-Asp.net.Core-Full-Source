using System.Security.Cryptography;

namespace DesignStudio.Core
{
    public static class Access
    {
        private const int Rounds = 100000;
        private const string Letters = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Rounds, HashAlgorithmName.SHA256, 32);
            return "pbkdf2$" + Rounds + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string stored)
        {
            string[] p = (stored ?? string.Empty).Split('$');
            if (p.Length != 4 || p[0] != "pbkdf2" || !int.TryParse(p[1], out int rounds))
            {
                return false;
            }
            try
            {
                byte[] salt = Convert.FromBase64String(p[2]);
                byte[] want = Convert.FromBase64String(p[3]);
                byte[] got = Rfc2898DeriveBytes.Pbkdf2(password, salt, rounds, HashAlgorithmName.SHA256, want.Length);
                return CryptographicOperations.FixedTimeEquals(want, got);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static string NewPassword()
        {
            char[] c = new char[14];
            int at = 0;
            for (int g = 0; g < 3; g++)
            {
                for (int i = 0; i < 4; i++)
                {
                    c[at++] = Letters[RandomNumberGenerator.GetInt32(Letters.Length)];
                }
                if (g < 2)
                {
                    c[at++] = '-';
                }
            }
            return new string(c);
        }
    }
}
