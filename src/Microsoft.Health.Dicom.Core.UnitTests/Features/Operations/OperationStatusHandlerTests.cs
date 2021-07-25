// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Operations
{
    public class OperationStatusHandlerTests
    {
        [Fact]
        public void GivenNullArgument_WhenConstructing_ThenThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OperationStatusHandler(null));
        }

        [Fact]
        public async Task GivenInvalidId_WhenHandlingRequest_ThenThrowArgumentException()
        {
            IDicomOperationsClient client = Substitute.For<IDicomOperationsClient>();
            await Assert.ThrowsAsync<ArgumentException>(
                () => new OperationStatusHandler(client).Handle(
                    new OperationStatusRequest(Guid.Empty),
                    CancellationToken.None));

            await client.DidNotReceiveWithAnyArgs().GetStatusAsync(default, default);
        }

        [Fact]
        public async Task GivenValidRequest_WhenHandlingRequest_ThenReturnResponse()
        {
            using var source = new CancellationTokenSource();
            IDicomOperationsClient client = Substitute.For<IDicomOperationsClient>();
            var handler = new OperationStatusHandler(client);

            Guid id = Guid.NewGuid();
            var expected = new OperationStatusResponse(id, OperationType.Reindex, DateTime.UtcNow, DateTime.UtcNow, OperationRuntimeStatus.Completed);
            client.GetStatusAsync(Arg.Is(id), Arg.Is(source.Token)).Returns(expected);

            Assert.Same(expected, await handler.Handle(new OperationStatusRequest(id), source.Token));

            await client.Received(1).GetStatusAsync(Arg.Is(id), Arg.Is(source.Token));
        }
    }
}
