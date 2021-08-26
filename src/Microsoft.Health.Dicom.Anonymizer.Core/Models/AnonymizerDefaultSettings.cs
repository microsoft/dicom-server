// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Models
{
    [DataContract]
    public class AnonymizerDefaultSettings
    {
        [DataMember(Name = "perturb")]
        public JObject PerturbDefaultSetting { get; set; }

        [DataMember(Name = "substitute")]
        public JObject SubstituteDefaultSetting { get; set; }

        [DataMember(Name = "dateshift")]
        public JObject DateShiftDefaultSetting { get; set; }

        [DataMember(Name = "encrypt")]
        public JObject EncryptDefaultSetting { get; set; }

        [DataMember(Name = "cryptoHash")]
        public JObject CryptoHashDefaultSetting { get; set; }

        [DataMember(Name = "redact")]
        public JObject RedactDefaultSetting { get; set; }

        public JObject GetDefaultSetting(string method)
        {
            return method.ToLower() switch
            {
                "perturb" => PerturbDefaultSetting,
                "substitute" => SubstituteDefaultSetting,
                "dateshift" => DateShiftDefaultSetting,
                "encrypt" => EncryptDefaultSetting,
                "cryptohash" => CryptoHashDefaultSetting,
                "redact" => RedactDefaultSetting,
                _ => null,
            };
        }
    }
}
