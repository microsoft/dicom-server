// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Blob.Utilities;

internal class ExternalBlobDataStoreConfiguration
{
    public const string SectionName = "ExternalBlobStore";

    public Uri BlobContainerUri { get; set; }

    // use for local testing with Azurite
    public string ConnectionString { get; set; }

    // use for local testing with Azurite
    public string ContainerName { get; set; }

    /// <summary>
    /// A path which is used to store blobs along a specific path in a container, serving as a prefix to the
    /// full blob name and providing a logical hierarchy when segments used though use of forward slashes (/).
    /// DICOM allows any alphanumeric characters, dashes(-). periods(.) and forward slashes (/) in the service store path.
    /// This path will be supplied externally when DICOM is a managed service.
    /// </summary>
    public string StorageDirectory { get; set; } = String.Empty;
}
