// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public interface IAnonymizerProcessorFactory
    {
        public IAnonymizerProcessor CreateProcessor(string anonymizeMethod, JObject ruleSetting = null);
    }
}
