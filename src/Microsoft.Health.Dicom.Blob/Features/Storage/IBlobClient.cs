// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;

/// <summary>
/// Responsible to get the right BlobContainerClient based on the configuration
/// </summary>
public interface IBlobClient
{
    // Making this a property to make the connection failures a API response failure instead of app/host initialization failure
    BlobContainerClient BlobContainerClient { get; }

    // To support SxS behavior of current internal store and tomorrows BYOS
    bool IsExternal { get; }
    string ServiceStorePath { get; }

    /// <summary>
    /// Checks that the client has been configured correctly and throws Exceptions if not.
    /// This allows us to surface configuration issues at startup through API calls.
    /// </summary>
    /// <exception cref="DataStoreException"></exception>
    void EnsureValidConfiguration();
}
