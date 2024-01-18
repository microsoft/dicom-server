// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Functions.ContentLengthBackFill;
using Microsoft.Health.Operations.Functions.DurableTask;
using NSubstitute;

namespace Microsoft.Health.Dicom.Functions.UnitTests.ContentLengthBackFill;

public partial class ContentLengthBackFillDurableFunctionTests
{
    private readonly ContentLengthBackFillDurableFunction _contentLengthBackFillDurableFunction;
    private readonly IInstanceStore _instanceStore;
    private readonly IIndexDataStore _indexStore;
    private readonly IFileStore _fileStore;
    private readonly ContentLengthBackFillOptions _options;

    public ContentLengthBackFillDurableFunctionTests()
    {
        _instanceStore = Substitute.For<IInstanceStore>();
        _indexStore = Substitute.For<IIndexDataStore>();
        _fileStore = Substitute.For<IFileStore>();
        _options = new ContentLengthBackFillOptions { RetryOptions = new ActivityRetryOptions() };
        _contentLengthBackFillDurableFunction = new ContentLengthBackFillDurableFunction(
            _instanceStore,
            _indexStore,
            _fileStore,
            Options.Create(_options));
    }
}
