// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Dicom;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Transaction;
using Microsoft.Health.Dicom.Transactional.Features.Storage;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomTransactionalTests : IClassFixture<DicomBlobStorageTestsFixture>
    {
        private readonly IDicomTransactionService _transactionService;

        public DicomTransactionalTests(DicomBlobStorageTestsFixture fixture)
        {
            ITransactionResolver transactionResolver = Substitute.For<ITransactionResolver>();
            transactionResolver.ResolveTransactionAsync(Arg.Any<ICloudBlob>()).Returns(Task.CompletedTask);
            _transactionService = new DicomTransactionService(fixture.CloudBlobClient, fixture.OptionsMonitor, transactionResolver, NullLogger<DicomTransactionService>.Instance);
        }

        [Fact]
        public async Task TestBeginTransaction()
        {
            var dicomInstance = new DicomInstance(DicomUID.Generate().UID, DicomUID.Generate().UID, DicomUID.Generate().UID);
            var dicomSeries = new DicomSeries(dicomInstance.StudyInstanceUID, dicomInstance.SeriesInstanceUID);

            using (ITransaction transaction = await _transactionService.BeginTransactionAsync(dicomSeries))
            {
                await transaction.AppendInstanceAsync(dicomInstance);
                await transaction.CommitAsync();
            }
        }
    }
}
