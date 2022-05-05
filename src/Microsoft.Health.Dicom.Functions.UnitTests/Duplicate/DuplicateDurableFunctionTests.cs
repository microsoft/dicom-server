// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Duplicate;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Functions.Duplicate;
using Microsoft.Health.Operations.Functions.DurableTask;
using NSubstitute;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Duplicate;

public partial class DuplicateDurableFunctionTests
{

    private readonly IInstanceStore _instanceStore;
    private readonly IInstanceDuplicater _instanceDuplicater;
    private readonly CopyDurableFunction _function;
    private readonly DuplicationOptions _options;

    public DuplicateDurableFunctionTests()
    {
        _options = new DuplicationOptions
        {
            BatchThreadCount = 1,
            BatchSize = 1,
            RetryOptions = new ActivityRetryOptions { MaxNumberOfAttempts = 5 }
        };
        _instanceStore = Substitute.For<IInstanceStore>();
        _instanceDuplicater = Substitute.For<IInstanceDuplicater>();

        _function = new CopyDurableFunction(_instanceStore, _instanceDuplicater, Options.Create(_options));
    }
}
