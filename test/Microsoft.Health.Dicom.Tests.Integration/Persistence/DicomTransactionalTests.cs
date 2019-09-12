// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Transaction;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomTransactionalTests : IClassFixture<DicomBlobStorageTestsFixture>
    {
        private readonly IDicomTransactionService _transactionService;

        public DicomTransactionalTests(DicomBlobStorageTestsFixture fixture)
        {
            _transactionService = fixture.DicomTransactionService;
        }

        [Fact]
        public async Task TestBeginTransaction()
        {
            var dicomInstance = new DicomInstance(DicomUID.Generate().UID, DicomUID.Generate().UID, DicomUID.Generate().UID);
            var dicomSeries = new DicomSeries(dicomInstance.StudyInstanceUID, dicomInstance.SeriesInstanceUID);

            using (ITransaction transaction = await _transactionService.BeginTransactionAsync(dicomSeries, dicomInstance))
            {
                await transaction.CommitAsync();
            }
        }
    }
}
