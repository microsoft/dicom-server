using System;
using System.IO;
using System.Security.Cryptography;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Utility
{
    public class EncryptUtility
    {
        // AES Initialization Vector length is 16 bytes
        private const int AesIvSize = 16;

        public static string EncryptTextToBase64WithAes(string plainText, byte[] key)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return plainText;
            }

            /* Create AES encryptor:
             * Mode: CBC
             * Block size: 16 bytes 
             * Acceptable key sizes： [128, 192, 256]
             */
            using Aes aes = Aes.Create();
            byte[] iv = aes.IV;
            aes.Key = key;
            using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            {

                // Get encrypted bytes
                using MemoryStream msEncrypt = new MemoryStream();
                using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
                byte[] encryptedBytes = msEncrypt.ToArray();

                // Concat IV to encrpted bytes
                byte[] result = new byte[AesIvSize + encryptedBytes.Length];
                Buffer.BlockCopy(iv, 0, result, 0, AesIvSize);
                Buffer.BlockCopy(encryptedBytes, 0, result, AesIvSize, encryptedBytes.Length);

                return Convert.ToBase64String(result);
            }
        }

        public static string DecryptTextFromBase64WithAes(string base64Text, byte[] key)
        {
            if (string.IsNullOrEmpty(base64Text))
            {
                return base64Text;
            }

            // Extract IV info from base64 text
            var byteData = Convert.FromBase64String(base64Text);
            if (byteData.Length < AesIvSize)
            {
                throw new FormatException($"The input base64Text for decryption should not be less than {AesIvSize} bytes length!");
            }

            var iv = new byte[16];
            Buffer.BlockCopy(byteData, 0, iv, 0, AesIvSize);
            var encryptedBytes = new byte[byteData.Length - AesIvSize];
            Buffer.BlockCopy(byteData, AesIvSize, encryptedBytes, 0, encryptedBytes.Length);

            // Get decryptor
            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            // Decrypt the cipher bytes.
            using MemoryStream msDecrypt = new MemoryStream(encryptedBytes);
            using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
    }
}
