// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class DicomProcessorFactory : IAnonymizerProcessorFactory
    {
        public IAnonymizerProcessor CreateProcessor(string anonymizeMethod, JObject ruleSetting = null)
        {
            return anonymizeMethod.ToLower() switch
            {
                "perturb" => new PerturbProcessor(ruleSetting),
                "substitute" => new SubstituteProcessor(ruleSetting),
                "dateshift" => new DateShiftProcessor(ruleSetting),
                "encrypt" => new EncryptProcessor(ruleSetting),
                "cryptohash" => new CryptoHashProcessor(ruleSetting),
                "redact" => new RedactProcessor(ruleSetting),
                "remove" => new RemoveProcessor(),
                "refreshuid" => new RefreshUIDProcessor(),
                "keep" => new KeepProcessor(),
                _ => null,
            };
        }
    }
}
