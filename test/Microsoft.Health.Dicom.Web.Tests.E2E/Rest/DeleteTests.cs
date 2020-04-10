// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Microsoft.IO;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class DeleteTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly DicomWebClient _client;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public DeleteTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
            _recyclableMemoryStreamManager = fixture.RecyclableMemoryStreamManager;
        }

        [Fact]
        public async Task GivenAnExistingInstance_WhenDeleting_TheServerShouldReturnNoContentAndAllLevelsShouldBeRemoved()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            HttpStatusCode deleteResult = await _client.DeleteAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            Assert.Equal(HttpStatusCode.NoContent, deleteResult);

            await VerifyRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
        }

        [Fact]
        public async Task GivenMultipleInstancesInSeries_WhenDeleting_TheServerShouldReturnNoContentAndOnlyTargetInstanceIsDeleted()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid1 = TestUidGenerator.Generate();
            var sopInstanceUid2 = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid1);
            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);

            HttpStatusCode deleteResult = await _client.DeleteAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid1);
            Assert.Equal(HttpStatusCode.NoContent, deleteResult);

            HttpResult<IReadOnlyList<DicomFile>> instanceResult = await _client.GetInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid1);
            Assert.Equal(HttpStatusCode.NotFound, instanceResult.StatusCode);

            HttpResult<IReadOnlyList<DicomFile>> nonDeletedInstanceResult = await _client.GetSeriesAsync(studyInstanceUid, seriesInstanceUid);
            Assert.Equal(HttpStatusCode.OK, nonDeletedInstanceResult.StatusCode);
            Assert.Equal(1, nonDeletedInstanceResult.Value.Count);
        }

        private async Task VerifyRemoval(string studyInstanceUid, string seriesInstanceUid = null, string sopInstanceUid = null)
        {
            if (!string.IsNullOrEmpty(sopInstanceUid))
            {
                HttpResult<IReadOnlyList<DicomFile>> instanceResult = await _client.GetInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
                Assert.Equal(HttpStatusCode.NotFound, instanceResult.StatusCode);
            }

            if (!string.IsNullOrEmpty(seriesInstanceUid))
            {
                HttpResult<IReadOnlyList<DicomFile>> seriesResult = await _client.GetSeriesAsync(studyInstanceUid, seriesInstanceUid);
                Assert.Equal(HttpStatusCode.NotFound, seriesResult.StatusCode);
            }

            HttpResult<IReadOnlyList<DicomFile>> studyResult = await _client.GetStudyAsync(studyInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, studyResult.StatusCode);
        }

        private async Task CreateFile(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            HttpResult<DicomDataset> response = await _client.PostAsync(new[] { dicomFile1 }, studyInstanceUid);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
