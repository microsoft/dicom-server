// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Functions.Migration;
using Microsoft.Health.Operations.Functions.DurableTask;
using NSubstitute;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Migration;

public partial class MigrationDurableFunctionTests
{
    private readonly MigrationFilesDurableFunction _migrationDurableFunction;
    private readonly IInstanceStore _instanceStore;
    private readonly IMetadataStore _metadataStore;
    private readonly MigrationFilesOptions _options;

    public MigrationDurableFunctionTests()
    {
        _instanceStore = Substitute.For<IInstanceStore>();
        _metadataStore = Substitute.For<IMetadataStore>();
        _options = new MigrationFilesOptions { RetryOptions = new ActivityRetryOptions() };
        _migrationDurableFunction = new MigrationFilesDurableFunction(
            _instanceStore,
            _metadataStore,
            Options.Create(_options));
    }
}
