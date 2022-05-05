// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Copy;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Copy;
using Microsoft.Health.Operations.Functions.DurableTask;
using NSubstitute;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Copy;

public partial class CopyDurableFunctionTests
{

    private readonly IInstanceStore _instanceStore;
    private readonly IInstanceCopier _instanceCopier;
    private readonly CopyDurableFunction _function;
    private readonly CopyOptions _options;
    private readonly BatchingOptions _batchingOptions;

    public CopyDurableFunctionTests()
    {
        _options = new CopyOptions
        {
            BatchThreadCount = 1,
            RetryOptions = new ActivityRetryOptions { MaxNumberOfAttempts = 5 }
        };
        _batchingOptions = new BatchingOptions
        {
            MaxParallelCount = 3,
            Size = 3
        };

        _instanceStore = Substitute.For<IInstanceStore>();
        _instanceCopier = Substitute.For<IInstanceCopier>();

        _function = new CopyDurableFunction(_instanceStore, _instanceCopier, Options.Create(_options));
    }
}
