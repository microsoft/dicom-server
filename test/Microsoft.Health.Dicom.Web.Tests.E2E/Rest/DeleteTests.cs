// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
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
        public async Task GivenAnExistingInstance_WhenDeletingInstance_TheServerShouldReturnNoContentAndAllLevelsShouldBeRemoved()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            HttpStatusCode deleteResult = await _client.DeleteAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            Assert.Equal(HttpStatusCode.NoContent, deleteResult);

            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
        }

        [Fact]
        public async Task GivenAnExistingInstance_WhenDeletingInstanceSecondTime_TheServerShouldReturnNotFound()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            HttpStatusCode deleteResult = await _client.DeleteAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            Assert.Equal(HttpStatusCode.NoContent, deleteResult);

            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            HttpStatusCode deleteSecondAttemptResult = await _client.DeleteAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, deleteSecondAttemptResult);
        }

        [Fact]
        public async Task GivenMultipleSeries_WhenDeletingInstance_TheServerShouldReturnNoContentAndOnlyTargetInstanceIsDeleted()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid1 = TestUidGenerator.Generate();
            var sopInstanceUid2 = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid1);
            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);

            HttpStatusCode deleteResult = await _client.DeleteAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid1);
            Assert.Equal(HttpStatusCode.NoContent, deleteResult);

            await VerifySopInstanceRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid1);

            await VerifyRemainingSeries(studyInstanceUid, seriesInstanceUid, 1);
        }

        [Fact]
        public async Task GivenMultipleSeriesInStudy_WhenDeletingInstance_TheServerShouldReturnNoContentAndOnlyTargetSeriesIsDeleted()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid1 = TestUidGenerator.Generate();
            var seriesInstanceUid2 = TestUidGenerator.Generate();
            var sopInstanceUid1 = TestUidGenerator.Generate();
            var sopInstanceUid2 = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid1, sopInstanceUid1);
            await CreateFile(studyInstanceUid, seriesInstanceUid2, sopInstanceUid2);

            HttpStatusCode deleteResult = await _client.DeleteAsync(studyInstanceUid, seriesInstanceUid1, sopInstanceUid1);
            Assert.Equal(HttpStatusCode.NoContent, deleteResult);

            await VerifySopInstanceRemoval(studyInstanceUid, seriesInstanceUid1, sopInstanceUid1);
            await VerifySeriesRemoval(studyInstanceUid, seriesInstanceUid1);

            await VerifyRemainingSeries(studyInstanceUid, seriesInstanceUid2, 1);
            await VerifyRemainingStudy(studyInstanceUid, 1);
        }

        [Fact]
        public async Task GivenMultipleInstancesInSeries_WhenDeletingInstance_TheServerShouldReturnNoContentAndOnlyTargetInstanceIsDeleted()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid1 = TestUidGenerator.Generate();
            var seriesInstanceUid2 = TestUidGenerator.Generate();
            var sopInstanceUid1 = TestUidGenerator.Generate();
            var sopInstanceUid2 = TestUidGenerator.Generate();
            var sopInstanceUid3 = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid1, sopInstanceUid1);
            await CreateFile(studyInstanceUid, seriesInstanceUid1, sopInstanceUid3);
            await CreateFile(studyInstanceUid, seriesInstanceUid2, sopInstanceUid2);

            HttpStatusCode deleteResult = await _client.DeleteAsync(studyInstanceUid, seriesInstanceUid1, sopInstanceUid1);
            Assert.Equal(HttpStatusCode.NoContent, deleteResult);

            await VerifySopInstanceRemoval(studyInstanceUid, seriesInstanceUid1, sopInstanceUid1);

            await VerifyRemainingSeries(studyInstanceUid, seriesInstanceUid1, 1);
            await VerifyRemainingSeries(studyInstanceUid, seriesInstanceUid2, 1);
            await VerifyRemainingStudy(studyInstanceUid, 2);
        }

        [Fact]
        public async Task GivenAnExistingSeries_WhenDeletingSeries_TheServerShouldReturnNoContentAndAllLevelsShouldBeRemoved()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            HttpStatusCode deleteResult = await _client.DeleteAsync(studyInstanceUid, seriesInstanceUid);
            Assert.Equal(HttpStatusCode.NoContent, deleteResult);

            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
        }

        [Fact]
        public async Task GivenAnExistingSeries_WhenDeletingSeriesSecondTime_TheServerShouldReturnNotFound()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            HttpStatusCode deleteResult = await _client.DeleteAsync(studyInstanceUid, seriesInstanceUid);
            Assert.Equal(HttpStatusCode.NoContent, deleteResult);

            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            HttpStatusCode deleteSecondAttemptResult = await _client.DeleteAsync(studyInstanceUid, seriesInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, deleteSecondAttemptResult);
        }

        [Fact]
        public async Task GivenAnMultipleInstancesInExistingSeries_WhenDeletingSeries_TheServerShouldReturnNoContentAndAllLevelsShouldBeRemoved()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid1 = TestUidGenerator.Generate();
            var sopInstanceUid2 = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid1);
            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);

            HttpStatusCode deleteResult = await _client.DeleteAsync(studyInstanceUid, seriesInstanceUid);
            Assert.Equal(HttpStatusCode.NoContent, deleteResult);

            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid1);
            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);
        }

        [Fact]
        public async Task GivenMultipleSeriesInStudy_WhenDeletingSeries_TheServerShouldReturnNoContentAndOnlyTargetSeriesIsDeleted()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid1 = TestUidGenerator.Generate();
            var seriesInstanceUid2 = TestUidGenerator.Generate();
            var sopInstanceUid1 = TestUidGenerator.Generate();
            var sopInstanceUid2 = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid1, sopInstanceUid1);
            await CreateFile(studyInstanceUid, seriesInstanceUid2, sopInstanceUid2);

            HttpStatusCode deleteResult = await _client.DeleteAsync(studyInstanceUid, seriesInstanceUid1, sopInstanceUid1);
            Assert.Equal(HttpStatusCode.NoContent, deleteResult);

            await VerifySopInstanceRemoval(studyInstanceUid, seriesInstanceUid1, sopInstanceUid1);
            await VerifySeriesRemoval(studyInstanceUid, seriesInstanceUid1);

            await VerifyRemainingSeries(studyInstanceUid, seriesInstanceUid2, 1);
            await VerifyRemainingStudy(studyInstanceUid, 1);
        }

        [Fact]
        public async Task GivenExistingStudy_WhenDeletingStudy_TheServerShouldReturnNoContentAndAllLevelsShouldBeRemoved()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            HttpStatusCode deleteResult = await _client.DeleteAsync(studyInstanceUid);
            Assert.Equal(HttpStatusCode.NoContent, deleteResult);

            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
        }

        [Fact]
        public async Task GivenAnExistingStudy_WhenDeletingStudySecondTime_TheServerShouldReturnNotFound()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            HttpStatusCode deleteResult = await _client.DeleteAsync(studyInstanceUid);
            Assert.Equal(HttpStatusCode.NoContent, deleteResult);

            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            HttpStatusCode deleteSecondAttemptResult = await _client.DeleteAsync(studyInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, deleteSecondAttemptResult);
        }

        [Fact]
        public async Task GivenExistingStudyWithMultipleItemsInEachLevel_WhenDeletingStudy_TheServerShouldReturnNoContentAndAllLevelsShouldBeRemoved()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid1 = TestUidGenerator.Generate();
            var seriesInstanceUid2 = TestUidGenerator.Generate();
            var sopInstanceUid1 = TestUidGenerator.Generate();
            var sopInstanceUid2 = TestUidGenerator.Generate();
            var sopInstanceUid3 = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid1, sopInstanceUid1);
            await CreateFile(studyInstanceUid, seriesInstanceUid2, sopInstanceUid2);
            await CreateFile(studyInstanceUid, seriesInstanceUid2, sopInstanceUid3);

            HttpStatusCode deleteResult = await _client.DeleteAsync(studyInstanceUid);
            Assert.Equal(HttpStatusCode.NoContent, deleteResult);

            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid1, sopInstanceUid1);
            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid2, sopInstanceUid2);
            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid2, sopInstanceUid3);
        }

        [Theory]
        [InlineData("notAStudyUid")]
        [InlineData("notAStudyUid", "notASeriesUid")]
        [InlineData("notAStudyUid", "notASeriesUid", "notASopInstanceUid")]
        [InlineData("2.25.106797093114774953545959916858814568441", "notASeriesUid")]
        [InlineData("2.25.106797093114774953545959916858814568441", "2.25.106797093114774953545959916858814568442", "notASopInstanceUid")]
        public async Task GivenABadUid_WhenDeleting_TheServerShouldReturnBackRequest(string studyUid = null, string seriesUid = null, string sopInstanceUid = null)
        {
            HttpStatusCode deleteBadRequestResult1 = await _client.DeleteAsync(studyUid, seriesUid, sopInstanceUid);
            Assert.Equal(HttpStatusCode.BadRequest, deleteBadRequestResult1);
        }

        private async Task VerifyAllRemoval(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            await VerifySopInstanceRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            await VerifySeriesRemoval(studyInstanceUid, seriesInstanceUid);
            await VerifyStudyRemoval(studyInstanceUid);
        }

        private async Task VerifyStudyRemoval(string studyInstanceUid)
        {
            HttpResult<IReadOnlyList<DicomFile>> studyResult = await _client.RetrieveStudyAsync(studyInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, studyResult.StatusCode);
        }

        private async Task VerifySeriesRemoval(string studyInstanceUid, string seriesInstanceUid)
        {
            HttpResult<IReadOnlyList<DicomFile>> seriesResult = await _client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, seriesResult.StatusCode);
        }

        private async Task VerifySopInstanceRemoval(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            HttpResult<IReadOnlyList<DicomFile>> instanceResult = await _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            Assert.Equal(HttpStatusCode.NotFound, instanceResult.StatusCode);
        }

        private async Task VerifyRemainingSeries(string studyInstanceUid, string seriesInstanceUid, int expectedInstanceCount)
        {
            HttpResult<IReadOnlyList<DicomFile>> seriesResult = await _client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid);
            Assert.Equal(HttpStatusCode.OK, seriesResult.StatusCode);
            Assert.Equal(expectedInstanceCount, seriesResult.Value.Count);
        }

        private async Task VerifyRemainingStudy(string studyInstanceUid, int expectedInstanceCount)
        {
            HttpResult<IReadOnlyList<DicomFile>> studyResult = await _client.RetrieveStudyAsync(studyInstanceUid);
            Assert.Equal(HttpStatusCode.OK, studyResult.StatusCode);
            Assert.Equal(expectedInstanceCount, studyResult.Value.Count);
        }

        private async Task CreateFile(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            HttpResult<DicomDataset> response = await _client.PostAsync(new[] { dicomFile1 }, studyInstanceUid);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
