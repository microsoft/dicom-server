// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Blob;

public static class Constants
{
    public const string BlobStoreConfigurationSection = "DicomWeb:DicomStore";
    public const string BlobContainerConfigurationName = "dicomBlob";

    public const string MetadataStoreConfigurationSection = "DicomWeb:MetadataStore";
    public const string MetadataContainerConfigurationName = "dicomMetadata";

    public const string SystemStoreConfigurationSection = "DicomWeb:SystemStore";
    public const string SystemContainerConfigurationName = "dicomSystem";

    public const string WorkitemStoreConfigurationSection = "DicomWeb:WorkitemStore";
    public const string WorkitemContainerConfigurationName = "dicomWorkitem";
}
