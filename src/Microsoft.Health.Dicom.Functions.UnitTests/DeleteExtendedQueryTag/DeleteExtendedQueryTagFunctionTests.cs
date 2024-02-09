// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag;
using Microsoft.Health.Operations.Functions.DurableTask;
using NSubstitute;

namespace Microsoft.Health.Dicom.Functions.UnitTests.DeleteExtendedQueryTag;

public partial class DeleteExtendedQueryTagFunctionTests
{
    private readonly DeleteExtendedQueryTagFunction _deleteExtendedQueryTagFunction;
    private readonly IExtendedQueryTagStore _extendedQueryTagStore;
    private readonly DeleteExtendedQueryTagOptions _options;

    public DeleteExtendedQueryTagFunctionTests()
    {
        _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
        _options = new DeleteExtendedQueryTagOptions { RetryOptions = new ActivityRetryOptions() };
        _deleteExtendedQueryTagFunction = new DeleteExtendedQueryTagFunction(
            _extendedQueryTagStore,
            Options.Create(_options));
    }
}
