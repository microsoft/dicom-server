// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DicomCast.Core.Configurations
{
    public class FeatureConfiguration
    {
        /// <summary>
        /// Do not sync values that are invalid and are not required
        /// </summary>
        public bool IgnoreSyncOfInvalidTagValue { get; set; }
    }
}
