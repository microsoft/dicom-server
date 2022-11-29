// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.BlobMigration;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;

namespace Microsoft.Health.Dicom.Functions.BlobMigration;

/// <summary>
/// Represents the Azure Durable Functions that copy existing DICOM instances to new format.
/// </summary>
public partial class CleanupDeletedDurableFunction
{
    private readonly IChangeFeedStore _changeFeedStore;
    private readonly BlobMigrationOptions _options;
    private readonly BlobMigrationService _blobMigrationService;

    public CleanupDeletedDurableFunction(
        IChangeFeedStore changeFeedStore,
        BlobMigrationService blobMigrationService,
        IOptions<BlobMigrationOptions> configOptions)
    {
        _changeFeedStore = EnsureArg.IsNotNull(changeFeedStore, nameof(changeFeedStore));
        _blobMigrationService = EnsureArg.IsNotNull(blobMigrationService, nameof(blobMigrationService));
        _options = EnsureArg.IsNotNull(configOptions?.Value, nameof(configOptions));
    }
}
