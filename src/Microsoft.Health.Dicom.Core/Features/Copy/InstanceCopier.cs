// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Copy;

/// <summary>
/// Represents an Instancecopier which copies the dicom instance in the same container
/// </summary>
public class InstanceCopier : IInstanceCopier
{
    private readonly IMetadataStore _metadataStore;
    private readonly IFileStore _fileStore;

    public InstanceCopier(
        IMetadataStore metadataStore,
        IFileStore fileStore)
    {
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
    }

    public async Task CopyInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

        await Task.WhenAll(
              _fileStore.CopyFileAsync(versionedInstanceIdentifier, cancellationToken),
              _metadataStore.CopyInstanceMetadataAsync(versionedInstanceIdentifier, cancellationToken));
    }
}
