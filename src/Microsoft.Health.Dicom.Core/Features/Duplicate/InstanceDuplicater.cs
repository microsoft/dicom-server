// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Duplicate;

/// <summary>
/// Represents an Reindexer which reindexes DICOM instance.
/// </summary>
public class InstanceDuplicator : IInstanceDuplicater
{
    private readonly IMetadataStore _metadataStore;
    private readonly IFileStore _fileStore;
    private readonly ILogger _logger;

    public InstanceDuplicator(
        IMetadataStore metadataStore,
        IFileStore fileStore,
        ILogger<InstanceDuplicator> logger)
    {
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task DuplicateInstanceAsync(VersionedInstanceIdentifier versionedInstanceId, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(versionedInstanceId, nameof(versionedInstanceId));
        await Task.WhenAll(
              _metadataStore.DuplicateInstanceMetadataAsync(versionedInstanceId, cancellationToken),
              _fileStore.DuplicateFileAsync(versionedInstanceId, cancellationToken));

        // TODO: Update  database if required
    }
}
