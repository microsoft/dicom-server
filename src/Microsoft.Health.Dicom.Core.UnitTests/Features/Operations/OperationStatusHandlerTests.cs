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
        public async Task GivenValidRequest_WhenHandlingRequest_ThenReturnResponse()
        {
            using var source = new CancellationTokenSource();
            IDicomOperationsClient client = Substitute.For<IDicomOperationsClient>();
            var handler = new OperationStatusHandler(client);

            Guid id = Guid.NewGuid();
            var expected = new OperationStatus<Uri>
            {
                CreatedTime = DateTime.UtcNow.AddMinutes(-5),
                LastUpdatedTime = DateTime.UtcNow,
                OperationId = id,
                PercentComplete = 100,
                Resources = new Uri[] { new Uri("https://dicom.contoso.io/unit/test/extendedquerytags/00101010", UriKind.Absolute) },
                Status = OperationRuntimeStatus.Completed,
                Type = OperationType.Reindex,
            };

            client.GetStatusAsync(Arg.Is(id), Arg.Is(source.Token)).Returns(expected);

            Assert.Same(expected, (await handler.Handle(new OperationStatusRequest(id), source.Token)).OperationStatus);

            await client.Received(1).GetStatusAsync(Arg.Is(id), Arg.Is(source.Token));
        }
    }
}
