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
}
