// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
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
                PollingInterval = 1,
            });
            _deletedInstanceCleanupWorker = new DeletedInstanceCleanupWorker(_dicomDeleteService, configuration, NullLogger<DeletedInstanceCleanupWorker>.Instance);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        [Fact]
        public async Task GivenLessThanTheBatchSize_WhenCallingExecute_ThenDeleteShouldBeCalledSingleTime()
        {
            _dicomDeleteService.CleanupDeletedInstancesAsync().ReturnsForAnyArgs((true, 9)).AndDoes(x => _cancellationTokenSource.Cancel());
            var executeTask = _deletedInstanceCleanupWorker.ExecuteAsync(_cancellationTokenSource.Token);

            await _dicomDeleteService.ReceivedWithAnyArgs(1).CleanupDeletedInstancesAsync();
        }

        [Theory]
        [InlineData(11, 2)]
        [InlineData(20, 3)]
        [InlineData(21, 3)]
        [InlineData(29, 3)]
        public void GivenMoreThanTheBatchSize_WhenCallingExecute_ThenDeleteShouldBeCalledCorrectNumberOfTimes(int numberOfDeletedInstances, int expectedDeleteCount)
        {
            (bool, int) GenerateResponse()
            {
                var returnValue = Math.Min(numberOfDeletedInstances, BatchSize);
                numberOfDeletedInstances = Math.Max(numberOfDeletedInstances - BatchSize, 0);

                return (true, returnValue);
            }

            _dicomDeleteService.CleanupDeletedInstancesAsync().ReturnsForAnyArgs(
                x => GenerateResponse())
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
