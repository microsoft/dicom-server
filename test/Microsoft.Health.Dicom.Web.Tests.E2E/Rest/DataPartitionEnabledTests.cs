// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Client;
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

            var newPartition = "partition1";

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

            var newPartition = "partition2";

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

            await _client.StoreAsync(new[] { dicomFile }, partitionName: newPartition1);
            await _client.StoreAsync(new[] { dicomFile }, partitionName: newPartition2);

            using DicomWebResponse<DicomFile> response1 = await _client.RetrieveInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, partitionName: newPartition1);
            Assert.True(response1.IsSuccessStatusCode);

            using DicomWebResponse<DicomFile> response2 = await _client.RetrieveInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, partitionName: newPartition2);
            Assert.True(response2.IsSuccessStatusCode);
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
