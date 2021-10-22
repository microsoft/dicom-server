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
    public class DataPartitionEnabledStoreTransactionTests : IClassFixture<DataPartitionEnabledHttpIntegrationTestFixture<Startup>>
    {
        private readonly IDicomWebClient _client;

        public DataPartitionEnabledStoreTransactionTests(DataPartitionEnabledHttpIntegrationTestFixture<Startup> fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            _client = fixture.Client;
        }

        [Fact]
        public async Task GivenDatasetWithNewPartitionName_WhenStoring_TheServerShouldReturnWithNewPartition()
        {
            var newPartition = "partition1";

            string studyInstanceUID = TestUidGenerator.Generate();

            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID);

            var response = await _client.StoreAsync(new[] { dicomFile }, partitionName: newPartition);

            Assert.True(response.IsSuccessStatusCode);

            ValidationHelpers.ValidateReferencedSopSequence(
                await response.GetValueAsync(),
                ConvertToReferencedSopSequenceEntry(dicomFile.Dataset, newPartition));
        }

        [Fact]
        public async Task GivenDatasetWithNewPartitionName_WhenStoringWithStudyUid_TheServerShouldReturnWithNewPartition()
        {
            var newPartition = "partition2";

            var studyInstanceUID = TestUidGenerator.Generate();
            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUid: studyInstanceUID);

            using DicomWebResponse<DicomDataset> response = await _client.StoreAsync(dicomFile, studyInstanceUID, newPartition);

            Assert.True(response.IsSuccessStatusCode);

            ValidationHelpers.ValidateReferencedSopSequence(
                await response.GetValueAsync(),
                ConvertToReferencedSopSequenceEntry(dicomFile.Dataset, newPartition));
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
