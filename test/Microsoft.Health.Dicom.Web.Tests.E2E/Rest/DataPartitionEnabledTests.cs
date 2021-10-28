// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class DataPartitionEnabledTests : IClassFixture<DataPartitionEnabledHttpIntegrationTestFixture<Startup>>
    {
        private readonly IDicomWebClient _client;
        private readonly bool _isUsingRemoteTestServer;

        public DataPartitionEnabledTests(DataPartitionEnabledHttpIntegrationTestFixture<Startup> fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            _client = fixture.Client;
            _isUsingRemoteTestServer = !fixture.IsUsingInProcTestServer;
        }

        [Fact]
        public async Task GivenDatasetWithNewPartitionName_WhenStoring_TheServerShouldReturnWithNewPartition()
        {
            if (_isUsingRemoteTestServer)
            {
                // Data partition feature flag only enabled locally. For Remote servers, feature flag is by default disabled
                return;
            }

            var newPartition = TestUidGenerator.Generate();

            string studyInstanceUID = TestUidGenerator.Generate();

            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID);

            using DicomWebResponse<DicomDataset> response = await _client.StoreAsync(new[] { dicomFile }, partitionName: newPartition);

            Assert.True(response.IsSuccessStatusCode);

            ValidationHelpers.ValidateReferencedSopSequence(
                await response.GetValueAsync(),
                ConvertToReferencedSopSequenceEntry(dicomFile.Dataset, newPartition));
        }

        [Fact]
        public async Task GivenDatasetWithNewPartitionName_WhenStoringWithStudyUid_TheServerShouldReturnWithNewPartition()
        {
            if (_isUsingRemoteTestServer)
            {
                // Data partition feature flag only enabled locally. For Remote servers, feature flag is by default disabled
                return;
            }

            var newPartition = TestUidGenerator.Generate();

            var studyInstanceUID = TestUidGenerator.Generate();
            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUid: studyInstanceUID);

            using DicomWebResponse<DicomDataset> response = await _client.StoreAsync(dicomFile, studyInstanceUID, newPartition);

            Assert.True(response.IsSuccessStatusCode);

            ValidationHelpers.ValidateReferencedSopSequence(
                await response.GetValueAsync(),
                ConvertToReferencedSopSequenceEntry(dicomFile.Dataset, newPartition));
        }

        [Fact]
        public async Task WhenRetrievingWithPartitionName_TheServerShouldReturnOnlyTheSpecifiedPartition()
        {
            if (_isUsingRemoteTestServer)
            {
                // Data partition feature flag only enabled locally. For Remote servers, feature flag is by default disabled
                return;
            }

            var newPartition1 = "partition1";
            var newPartition2 = "partition2";

            string studyInstanceUID = TestUidGenerator.Generate();
            string seriesInstanceUID = TestUidGenerator.Generate();
            string sopInstanceUID = TestUidGenerator.Generate();

            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID, seriesInstanceUID, sopInstanceUID);

            using DicomWebResponse<DicomDataset> response1 = await _client.StoreAsync(new[] { dicomFile }, partitionName: newPartition1);
            using DicomWebResponse<DicomDataset> response2 = await _client.StoreAsync(new[] { dicomFile }, partitionName: newPartition2);

            using DicomWebResponse<DicomFile> response3 = await _client.RetrieveInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, partitionName: newPartition1);
            Assert.True(response3.IsSuccessStatusCode);

            using DicomWebResponse<DicomFile> response4 = await _client.RetrieveInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, partitionName: newPartition2);
            Assert.True(response4.IsSuccessStatusCode);
        }

        [Fact]
        public async Task GivenDatasetInstancesWithDifferentPartitions_WhenDeleted_OneDeletedAndOtherRemains()
        {
            if (_isUsingRemoteTestServer)
            {
                // Data partition feature flag only enabled locally. For Remote servers, feature flag is by default disabled
                return;
            }

            var newPartition1 = TestUidGenerator.Generate();
            var newPartition2 = TestUidGenerator.Generate();

            string studyInstanceUID = TestUidGenerator.Generate();
            string seriesInstanceUID = TestUidGenerator.Generate();
            string sopInstanceUID = TestUidGenerator.Generate();

            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID, seriesInstanceUID, sopInstanceUID);

            using DicomWebResponse<DicomDataset> response1 = await _client.StoreAsync(new[] { dicomFile }, partitionName: newPartition1);
            using DicomWebResponse<DicomDataset> response2 = await _client.StoreAsync(new[] { dicomFile }, partitionName: newPartition2);

            using DicomWebResponse response3 = await _client.DeleteInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, newPartition1);
            Assert.True(response3.IsSuccessStatusCode);

            await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, partitionName: newPartition1));

            using DicomWebResponse<DicomFile> response5 = await _client.RetrieveInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, partitionName: newPartition2);
            Assert.True(response5.IsSuccessStatusCode);
        }

        [Fact]
        public async Task GivenAnInstance_WhenRetrievingChangeFeedWithPartition_ThenPartitionNameIsReturned()
        {
            if (_isUsingRemoteTestServer)
            {
                // Data partition feature flag only enabled locally. For Remote servers, feature flag is by default disabled
                return;
            }

            var newPartition = TestUidGenerator.Generate();
            string studyInstanceUID = TestUidGenerator.Generate();
            string seriesInstanceUID = TestUidGenerator.Generate();
            string sopInstanceUID = TestUidGenerator.Generate();


            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID, seriesInstanceUID, sopInstanceUID);
            using DicomWebResponse<DicomDataset> response1 = await _client.StoreAsync(new[] { dicomFile }, partitionName: newPartition);

            long initialSequence;

            using (DicomWebResponse<ChangeFeedEntry> response = await _client.GetChangeFeedLatest("?includemetadata=false"))
            {
                ChangeFeedEntry changeFeedEntry = await response.GetValueAsync();

                initialSequence = changeFeedEntry.Sequence;

                Assert.Equal(newPartition, changeFeedEntry.PartitionName);
                Assert.Equal(studyInstanceUID, changeFeedEntry.StudyInstanceUid);
                Assert.Equal(seriesInstanceUID, changeFeedEntry.SeriesInstanceUid);
                Assert.Equal(sopInstanceUID, changeFeedEntry.SopInstanceUid);
            }

            using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await _client.GetChangeFeed($"?offset={initialSequence - 1}&includemetadata=false"))
            {
                ChangeFeedEntry[] changeFeedResults = await response.ToArrayAsync();

                Assert.Single(changeFeedResults);
                Assert.Null(changeFeedResults[0].Metadata);
                Assert.Equal(newPartition, changeFeedResults[0].PartitionName);
                Assert.Equal(ChangeFeedState.Current, changeFeedResults[0].State);
            }
        }

        private (string SopInstanceUid, string RetrieveUri, string SopClassUid) ConvertToReferencedSopSequenceEntry(DicomDataset dicomDataset, string partitionName)
        {
            string studyInstanceUid = dicomDataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            string seriesInstanceUid = dicomDataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
            string sopInstanceUid = dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

            string relativeUri = $"{DicomApiVersions.Latest}/partitions/{partitionName}/studies/{studyInstanceUid}/series/{seriesInstanceUid}/instances/{sopInstanceUid}";

            return (dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID),
                new Uri(_client.HttpClient.BaseAddress, relativeUri).ToString(),
                dicomDataset.GetSingleValue<string>(DicomTag.SOPClassUID));
        }
    }
}
