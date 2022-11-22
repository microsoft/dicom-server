// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.BlobMigration;

/// <summary>
/// Represents an BlobMigrationService which copies the dicom instance in the same container and deletes old blobs
/// </summary>
public class BlobMigrationService : IBlobMigrationService
{
    private readonly IMetadataStore _metadataStore;
    private readonly IFileStore _fileStore;

    public BlobMigrationService(IMetadataStore metadataStore, IFileStore fileStore)
    {
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
    }

    public Task CopyInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        return Task.WhenAll(
              _fileStore.CopyFileAsync(versionedInstanceIdentifier, cancellationToken),
              _metadataStore.CopyInstanceMetadataAsync(versionedInstanceIdentifier, cancellationToken));
    }

    public Task DeleteInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, bool forceDelete = false, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        return Task.WhenAll(
              _fileStore.DeleteOldFileIfExistsAsync(versionedInstanceIdentifier, forceDelete, cancellationToken),
              _metadataStore.DeleteOldInstanceMetadataIfExistsAsync(versionedInstanceIdentifier, forceDelete, cancellationToken));
    }
}
