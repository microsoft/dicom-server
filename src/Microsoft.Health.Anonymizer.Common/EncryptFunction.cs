// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using EnsureThat;
using Microsoft.Health.Anonymizer.Common.Exceptions;
using Microsoft.Health.Anonymizer.Common.Settings;

namespace Microsoft.Health.Anonymizer.Common
{
    public class EncryptFunction
    {
        // AES Initialization Vector length is 16 bytes
        private const int AesIvSize = 16;
        private readonly byte[] _aesKey;

        public EncryptFunction(EncryptSetting encryptSetting)
        {
            EnsureArg.IsNotNull(encryptSetting, nameof(encryptSetting));

            encryptSetting.Validate();
            _aesKey = encryptSetting.GetEncryptByteKey();
        }

        public byte[] Encrypt(string plainText, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(plainText, nameof(plainText));

            if (plainText == string.Empty)
            {
                return new byte[] { };
            }

            encoding ??= Encoding.UTF8;
            return Encrypt(encoding.GetBytes(plainText));
        }

        public byte[] Encrypt(Stream plainStream)
        {
            EnsureArg.IsNotNull(plainStream, nameof(plainStream));

            return Encrypt(StreamToByte(plainStream));
        }

        public byte[] Encrypt(byte[] plainBytes)
        {
            EnsureArg.IsNotNull(plainBytes, nameof(plainBytes));

            /* Create AES encryptor:
             * Mode: CBC
             * Block size: 16 bytes
             * Acceptable key sizes： [128, 192, 256]
             */
            try
            {
                using Aes aes = Aes.Create();
                byte[] iv = aes.IV;
                aes.Key = _aesKey;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] encryptedBytes;

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(plainBytes);
                    }

                    encryptedBytes = msEncrypt.ToArray();
                }

                // Concat IV to encrypted bytes
                byte[] result = new byte[AesIvSize + encryptedBytes.Length];
                Buffer.BlockCopy(iv, 0, result, 0, AesIvSize);
                Buffer.BlockCopy(encryptedBytes, 0, result, AesIvSize, encryptedBytes.Length);

                return result;
            }
            catch (Exception ex)
            {
                throw new AnonymizerException(AnonymizerErrorCode.EncryptFailed, "Failed to encrypt data.", ex);
            }
        }

        public byte[] Decrypt(string cipherText, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(cipherText, nameof(cipherText));

            if (cipherText == string.Empty)
            {
                return new byte[] { };
            }

            encoding ??= Encoding.UTF8;
            var byteData = encoding.GetBytes(cipherText);
            return Decrypt(byteData);
        }

        public byte[] Decrypt(Stream cipherStream)
        {
            EnsureArg.IsNotNull(cipherStream, nameof(cipherStream));

            return Decrypt(StreamToByte(cipherStream));
        }

        public byte[] Decrypt(byte[] cipherBytes)
        {
            EnsureArg.IsNotNull(cipherBytes, nameof(cipherBytes));

            if (cipherBytes.Length == 0)
            {
                return cipherBytes;
            }

            // Extract IV info from base64 text

            if (cipherBytes.Length < AesIvSize)
            {
                throw new FormatException($"The input text for decryption should not be less than {AesIvSize} bytes length!");
            }

            try
            {
                var iv = new byte[AesIvSize];
                Buffer.BlockCopy(cipherBytes, 0, iv, 0, AesIvSize);
                var encryptedBytes = new byte[cipherBytes.Length - AesIvSize];
                Buffer.BlockCopy(cipherBytes, AesIvSize, encryptedBytes, 0, encryptedBytes.Length);

                // Get decryptor
                using Aes aes = Aes.Create();
                aes.Key = _aesKey;
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using MemoryStream msDecrypt = new MemoryStream(encryptedBytes);
                using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);

                return StreamToByte(csDecrypt);
            }
            catch (Exception ex)
            {
                throw new AnonymizerException(AnonymizerErrorCode.EncryptFailed, "Failed to decrypt data.", ex);
            }
        }

        private byte[] StreamToByte(Stream inputStream)
        {
            EnsureArg.IsNotNull(inputStream, nameof(inputStream));

            using var memStream = new MemoryStream();
            inputStream.CopyTo(memStream);
            return memStream.ToArray();
        }
    }
}
