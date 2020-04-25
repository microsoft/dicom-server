// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Features.BackgroundServices;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Delete;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.BackgroundServices
{
    public class DeletedInstanceCleanupWorkerTests
    {
        private readonly DeletedInstanceCleanupWorker _deletedInstanceCleanupWorker;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IDicomDeleteService _dicomDeleteService;
        private const int BatchSize = 10;

        public DeletedInstanceCleanupWorkerTests()
        {
            _dicomDeleteService = Substitute.For<IDicomDeleteService>();
            var configuration = Substitute.For<IOptions<DeletedInstanceCleanupConfiguration>>();
            configuration.Value.Returns(new DeletedInstanceCleanupConfiguration
            {
                BatchSize = BatchSize,
                PollingInterval = TimeSpan.FromSeconds(1),
            });
            _deletedInstanceCleanupWorker = new DeletedInstanceCleanupWorker(_dicomDeleteService, configuration, NullLogger<DeletedInstanceCleanupWorker>.Instance);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(9, 1)]
        [InlineData(10, 2)]
        [InlineData(11, 2)]
        [InlineData(19, 2)]
        [InlineData(20, 3)]
        [InlineData(21, 3)]
        public void GivenANumberOfDeletedEntriesAndBatchSize_WhenCallingExecute_ThenDeleteShouldBeCalledCorrectNumberOfTimes(int numberOfDeletedInstances, int expectedDeleteCount)
        {
            (bool, int) GenerateCleanupDeletedInstancesAsyncResponse()
            {
                var returnValue = Math.Min(numberOfDeletedInstances, BatchSize);
                numberOfDeletedInstances = Math.Max(numberOfDeletedInstances - BatchSize, 0);

                return (true, returnValue);
            }

            _dicomDeleteService.CleanupDeletedInstancesAsync().ReturnsForAnyArgs(
                x => GenerateCleanupDeletedInstancesAsyncResponse())
                .AndDoes(x =>
                {
                    if (numberOfDeletedInstances == 0)
                    {
                        _cancellationTokenSource.Cancel();
                    }
                });

            _deletedInstanceCleanupWorker.ExecuteAsync(_cancellationTokenSource.Token);
            _dicomDeleteService.ReceivedWithAnyArgs(expectedDeleteCount).CleanupDeletedInstancesAsync();
        }
    }
}
