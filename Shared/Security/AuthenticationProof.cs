using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Security.Cryptography;
using System.Text;

namespace Shared.Security
{
    public static class AuthenticationProof
    {
        public static string CreateChallenge()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);

            return Convert.ToBase64String(bytes);
        }

        public static string CreateProof(
            string secretKey,
            string challenge)
        {
            byte[] key = Convert.FromBase64String(secretKey);

            if (key.Length != 32)
            {
                throw new ArgumentException(
                    "Khóa xác thực phải có 32 byte.");
            }

            byte[] data = Encoding.UTF8.GetBytes(challenge);

            byte[] proof = HMACSHA256.HashData(key, data);

            return Convert.ToBase64String(proof);
        }

        public static bool VerifyProof(
            string secretKey,
            string challenge,
            string receivedProof)
        {
            string expectedProof = CreateProof(
                secretKey,
                challenge);

            byte[] receivedBytes;

            try
            {
                receivedBytes =
                    Convert.FromBase64String(receivedProof);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] expectedBytes =
                Convert.FromBase64String(expectedProof);

            return CryptographicOperations.FixedTimeEquals(
                expectedBytes,
                receivedBytes);
        }
    }
}