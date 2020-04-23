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

            DicomWebResponse response = await _client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
        }

        [Fact]
        public async Task GivenAnExistingInstance_WhenDeletingInstanceSecondTime_TheServerShouldReturnNotFound()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomWebResponse response = await _client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
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

            await _client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid1);

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

            await _client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid1, sopInstanceUid1);

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

            await _client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid1, sopInstanceUid1);

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

            DicomWebResponse response = await _client.DeleteSeriesAsync(studyInstanceUid, seriesInstanceUid);

            Assert.Equal(HttpStatusCode.NoContent, response?.StatusCode);
            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
        }

        [Fact]
        public async Task GivenAnExistingSeries_WhenDeletingSeriesSecondTime_TheServerShouldReturnNotFound()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            await _client.DeleteSeriesAsync(studyInstanceUid, seriesInstanceUid);

            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.DeleteSeriesAsync(studyInstanceUid, seriesInstanceUid));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
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

            await _client.DeleteSeriesAsync(studyInstanceUid, seriesInstanceUid);

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

            await _client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid1, sopInstanceUid1);

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

            DicomWebResponse response = await _client.DeleteStudyAsync(studyInstanceUid);

            Assert.Equal(HttpStatusCode.NoContent, response?.StatusCode);
            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
        }

        [Fact]
        public async Task GivenAnExistingStudy_WhenDeletingStudySecondTime_TheServerShouldReturnNotFound()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            await CreateFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            await _client.DeleteStudyAsync(studyInstanceUid);

            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.DeleteStudyAsync(studyInstanceUid));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
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

            await _client.DeleteStudyAsync(studyInstanceUid);

            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid1, sopInstanceUid1);
            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid2, sopInstanceUid2);
            await VerifyAllRemoval(studyInstanceUid, seriesInstanceUid2, sopInstanceUid3);
        }

        [Theory]
        [InlineData("notAStudyUid")]
        public async Task GivenABadStudyUid_WhenDeleting_TheServerShouldReturnBackRequest(string studyUid)
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.DeleteStudyAsync(studyUid));
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        [Theory]
        [InlineData("notAStudyUid", "notASeriesUid")]
        [InlineData("2.25.106797093114774953545959916858814568441", "notASeriesUid")]
        public async Task GivenABadSeriesUid_WhenDeleting_TheServerShouldReturnBackRequest(string studyUid, string seriesUid)
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.DeleteSeriesAsync(studyUid, seriesUid));
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        [Theory]
        [InlineData("notAStudyUid", "notASeriesUid", "notASopInstanceUid")]
        [InlineData("2.25.106797093114774953545959916858814568441", "2.25.106797093114774953545959916858814568442", "notASopInstanceUid")]
        public async Task GivenABadInstanceUid_WhenDeleting_TheServerShouldReturnBackRequest(string studyUid, string seriesUid, string instanceUid)
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.DeleteInstanceAsync(studyUid, seriesUid, instanceUid));
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        private async Task VerifyAllRemoval(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            await VerifySopInstanceRemoval(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            await VerifySeriesRemoval(studyInstanceUid, seriesInstanceUid);
            await VerifyStudyRemoval(studyInstanceUid);
        }

        private async Task VerifyStudyRemoval(string studyInstanceUid)
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveStudyAsync(studyInstanceUid));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        private async Task VerifySeriesRemoval(string studyInstanceUid, string seriesInstanceUid)
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        private async Task VerifySopInstanceRemoval(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        private async Task VerifyRemainingSeries(string studyInstanceUid, string seriesInstanceUid, int expectedInstanceCount)
        {
            DicomWebResponse<IReadOnlyList<DicomFile>> seriesResult = await _client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid);

            Assert.Equal(expectedInstanceCount, seriesResult.Value.Count);
        }

        private async Task VerifyRemainingStudy(string studyInstanceUid, int expectedInstanceCount)
        {
            DicomWebResponse<IReadOnlyList<DicomFile>> studyResult = await _client.RetrieveStudyAsync(studyInstanceUid);

            Assert.Equal(expectedInstanceCount, studyResult.Value.Count);
        }

        private async Task CreateFile(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            await _client.StoreAsync(new[] { dicomFile1 }, studyInstanceUid);
        }
    }
}
