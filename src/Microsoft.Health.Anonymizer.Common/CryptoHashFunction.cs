// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using EnsureThat;
using Microsoft.Health.Anonymizer.Common.Exceptions;
using Microsoft.Health.Anonymizer.Common.Settings;

namespace Microsoft.Health.Anonymizer.Common
{
    public class CryptoHashFunction
    {
        private readonly HMAC _hmac;

        public CryptoHashFunction(CryptoHashSetting cryptoHashSetting)
        {
            EnsureArg.IsNotNull(cryptoHashSetting, nameof(cryptoHashSetting));

            byte[] byteKey = cryptoHashSetting.GetCryptoHashByteKey();
            _hmac = cryptoHashSetting.CryptoHashType switch
            {
                HashAlgorithmType.Md5 => new HMACMD5(byteKey),
                HashAlgorithmType.Sha1 => new HMACSHA1(byteKey),
                HashAlgorithmType.Sha256 => new HMACSHA256(byteKey),
                HashAlgorithmType.Sha512 => new HMACSHA512(byteKey),
                HashAlgorithmType.Sha384 => new HMACSHA384(byteKey),
                _ => throw new AnonymizerException(AnonymizerErrorCode.CryptoHashFailed, "Hash function not supported."),
            };
        }

        public byte[] Hash(byte[] input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            return Hash(input, _hmac);
        }

        public byte[] Hash(Stream input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            return Hash(input, _hmac);
        }

        public byte[] Hash(string input, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            return Hash(input, _hmac, encoding);
        }

        public static byte[] Hash(byte[] input, HMAC hashAlgorithm)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            return hashAlgorithm.ComputeHash(input);
        }

        public static byte[] Hash(string input, HMAC hashAlgorithm, Encoding encoding = null)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            encoding ??= Encoding.UTF8;
            return hashAlgorithm.ComputeHash(encoding.GetBytes(input));
        }

        public static byte[] Hash(Stream input, HMAC hashAlgorithm)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(hashAlgorithm, nameof(hashAlgorithm));

            return hashAlgorithm.ComputeHash(input);
        }
    }
}
