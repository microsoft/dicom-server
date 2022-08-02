// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.BlobMigration;
using Microsoft.Health.Dicom.Core.Features.Retrieve;

namespace Microsoft.Health.Dicom.Functions.BlobMigration;

/// <summary>
/// Represents the Azure Durable Functions that copy existing DICOM instances to new format.
/// </summary>
public partial class DeleteDurableFunction
{
    private readonly IInstanceStore _instanceStore;
    private readonly BlobMigrationOptions _options;
    private readonly BlobMigrationService _blobMigrationService;

    public DeleteDurableFunction(
        IInstanceStore instanceStore,
        BlobMigrationService blobMigrationService,
        IOptions<BlobMigrationOptions> configOptions)
    {
        _instanceStore = EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
        _blobMigrationService = EnsureArg.IsNotNull(blobMigrationService, nameof(blobMigrationService));
        _options = EnsureArg.IsNotNull(configOptions?.Value, nameof(configOptions));
        EnsureArg.IsNotNull(_options.RetryOptions, nameof(_options.RetryOptions));
    }
}
