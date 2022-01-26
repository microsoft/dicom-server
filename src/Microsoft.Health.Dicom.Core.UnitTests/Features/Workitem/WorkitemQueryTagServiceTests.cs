// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ExtendedQueryTag
{
    public class WorkitemQueryTagServiceTests
    {
        private readonly IIndexWorkitemStore _indexWorkitemStore;
        private readonly IWorkitemQueryTagService _queryTagService;
        private readonly IDicomTagParser _dicomTagParser;

        public WorkitemQueryTagServiceTests()
        {
            _indexWorkitemStore = Substitute.For<IIndexWorkitemStore>();
            _dicomTagParser = Substitute.For<IDicomTagParser>();
            _queryTagService = new WorkitemQueryTagService(_indexWorkitemStore, _dicomTagParser, NullLogger<WorkitemQueryTagService>.Instance);
        }

        [Fact]
        public async Task GivenValidInput_WhenGetWorkitemQueryTagsIsCalledMultipleTimes_ThenWorkitemStoreIsCalledOnce()
        {
            _indexWorkitemStore.GetWorkitemQueryTagsAsync(Arg.Any<CancellationToken>())
                  .Returns(Array.Empty<WorkitemQueryTagStoreEntry>());

            await _queryTagService.GetQueryTagsAsync();
            await _queryTagService.GetQueryTagsAsync();
            await _indexWorkitemStore.Received(1).GetWorkitemQueryTagsAsync(Arg.Any<CancellationToken>());
        }
    }
}
