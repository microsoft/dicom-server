// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.BlobMigration;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Functions.BlobMigration;
using Microsoft.Health.Operations.Functions.DurableTask;
using NSubstitute;

namespace Microsoft.Health.Dicom.Functions.UnitTests.CleanupDeleted;

public partial class CleanupDeletedDurableFunctionTests
{
    private readonly IChangeFeedStore _changeFeedStore;
    private readonly BlobMigrationService _blobMigrationService;
    private readonly CleanupDeletedDurableFunction _function;
    private readonly BlobMigrationOptions _options;
    private readonly BatchingOptions _batchingOptions;

    public CleanupDeletedDurableFunctionTests()
    {
        _options = new BlobMigrationOptions
        {
            MaxParallelThreads = 1,
            RetryOptions = new ActivityRetryOptions { MaxNumberOfAttempts = 5 }
        };
        _batchingOptions = new BatchingOptions
        {
            MaxParallelCount = 3,
            Size = 3
        };

        _changeFeedStore = Substitute.For<IChangeFeedStore>();
        _blobMigrationService = Substitute.For<BlobMigrationService>(Substitute.For<IMetadataStore>(), Substitute.For<IFileStore>());

        _function = new CleanupDeletedDurableFunction(_changeFeedStore, _blobMigrationService, Options.Create(_options));
    }
}
