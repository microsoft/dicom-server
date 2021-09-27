// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Configs
{
    public class StoreConfiguration
    {
        /// <summary>
        /// Maximum allowed request length per dicom file
        /// </summary>
        public long MaxAllowedDicomFileSize { get; set; } = 2147483648;
    }
}
