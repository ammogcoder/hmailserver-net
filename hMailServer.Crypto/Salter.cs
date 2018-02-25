using System.Security.Cryptography;
using System.Text;

namespace hMailServer.Crypto
{
    public class Salter
    {
        private const int SaltLength = 6;

        public bool ValidateHash(string password, string originalHash)
        {
            string salt = originalHash.Substring(0, SaltLength);

            string newHash = salt + Sha256(salt + password);

            if (newHash == originalHash)
                return true;

            return false;
        }


        private string Sha256(string value)
        {
            using (var sha256Managed = new SHA256Managed())
            {
                var result = new StringBuilder();
                byte[] hash = sha256Managed.ComputeHash(Encoding.UTF8.GetBytes(value), 0, Encoding.UTF8.GetByteCount(value));

                foreach (byte byteInHash in hash)
                    result.Append(byteInHash.ToString("x2"));

                return result.ToString();
            }
        }
    }
}
