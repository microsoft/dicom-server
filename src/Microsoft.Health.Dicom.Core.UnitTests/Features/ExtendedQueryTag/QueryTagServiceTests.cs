// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ExtendedQueryTag;

public class QueryTagServiceTests
{
    private readonly IExtendedQueryTagStore _extendedQueryTagStore;
    private readonly IQueryTagService _queryTagService;

    public QueryTagServiceTests()
    {
        _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
        _queryTagService = new QueryTagService(_extendedQueryTagStore);
    }

    [Fact]
    public async Task GivenValidInput_WhenGetExtendedQueryTagsIsCalledMultipleTimes_ThenExtendedQueryTagStoreIsCalledOnce()
    {
        _extendedQueryTagStore.GetExtendedQueryTagsAsync(int.MaxValue, 0, Arg.Any<CancellationToken>())
              .Returns(Array.Empty<ExtendedQueryTagStoreJoinEntry>());

        await _queryTagService.GetQueryTagsAsync();
        await _queryTagService.GetQueryTagsAsync();
        await _extendedQueryTagStore.Received(1).GetExtendedQueryTagsAsync(int.MaxValue, 0, Arg.Any<CancellationToken>());
    }
}
