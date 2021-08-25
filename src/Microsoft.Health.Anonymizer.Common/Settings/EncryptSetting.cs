// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using Microsoft.Health.Anonymizer.Common.Exceptions;

namespace Microsoft.Health.Anonymizer.Common.Settings
{
    public class EncryptSetting
    {
        public string EncryptKey { private get; set; }

        public void Validate()
        {
            var encryptKeySize = GetEncryptByteKey().Length * 8;
            if (encryptKeySize != 128 && encryptKeySize != 192 && encryptKeySize != 256)
            {
                throw new AnonymizerException(
                    AnonymizerErrorCode.InvalidAnonymizerSettings,
                    $"Invalid encrypt key size : {encryptKeySize} bits! Please provide key sizes of 128, 192 or 256 bits.");
            }
        }

        public byte[] GetEncryptByteKey()
        {
            return EncryptKey == null ? Aes.Create().Key : Encoding.UTF8.GetBytes(EncryptKey);
        }
    }
}
