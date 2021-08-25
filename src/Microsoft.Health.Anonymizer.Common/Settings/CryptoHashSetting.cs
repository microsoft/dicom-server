// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Health.Anonymizer.Common.Settings
{
    public class CryptoHashSetting
    {
        public string CryptoHashKey { private get; set; }

        public HashAlgorithmType CryptoHashType { get; set; } = HashAlgorithmType.Sha256;

        public byte[] GetCryptoHashByteKey()
        {
            return CryptoHashKey == null ? Aes.Create().Key : Encoding.UTF8.GetBytes(CryptoHashKey);
        }
    }
}
