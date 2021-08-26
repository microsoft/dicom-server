using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility
{
    public class CryptoHashUtility
    {
        public static string ComputeHmacSHA256Hash(string input, string hashKey)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var key = Encoding.UTF8.GetBytes(hashKey);
            using var hmac = new HMACSHA256(key);
            var plainData = Encoding.UTF8.GetBytes(input);
            var hashData = hmac.ComputeHash(plainData);

            return string.Concat(hashData.Select(b => b.ToString("x2")));
        }
    }
}
