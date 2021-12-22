// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------


using Microsoft.Health.Dicom.Blob.Utilities;

namespace Microsoft.Health.Dicom.Metadata.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class BlobContainerConfigurationAware : StoreConfigurationAware
    {
        internal const string ConfigurationSectionName = "DicomWeb:DicomStore";
        internal const string ContainerName = "dicomBlob";

        public BlobContainerConfigurationAware()
            : base(ConfigurationSectionName, ContainerName)
        {
        }
    }
}
