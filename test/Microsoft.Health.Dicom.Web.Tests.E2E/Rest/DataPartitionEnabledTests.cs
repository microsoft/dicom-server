// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Xunit;
using DefaultPartition = Microsoft.Health.Dicom.Client.Models.DefaultPartition;
using PartitionEntry = Microsoft.Health.Dicom.Client.Models.PartitionEntry;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public class DataPartitionEnabledTests : IClassFixture<DataPartitionEnabledHttpIntegrationTestFixture<Startup>>, IAsyncLifetime
{
    private readonly IDicomWebClient _client;
    private readonly DicomInstancesManager _instancesManager;

    public DataPartitionEnabledTests(DataPartitionEnabledHttpIntegrationTestFixture<Startup> fixture)
    {
        EnsureArg.IsNotNull(fixture, nameof(fixture));
        _client = fixture.GetDicomWebClient();
        _instancesManager = new DicomInstancesManager(_client);
    }

    [Fact]
    [Trait("Category", "bvt-dp")]
    public async Task WhenRetrievingPartitions_TheServerShouldReturnAllPartitions()
    {
        var newPartition1 = new PartitionEntry(DefaultPartition.Key, TestUidGenerator.Generate());
        var newPartition2 = new PartitionEntry(DefaultPartition.Key, TestUidGenerator.Generate());

        DicomFile dicomFile = Samples.CreateRandomDicomFile();

        using DicomWebResponse<DicomDataset> response1 = await _instancesManager.StoreAsync(new[] { dicomFile }, partitionEntry: newPartition1);
        using DicomWebResponse<DicomDataset> response2 = await _instancesManager.StoreAsync(new[] { dicomFile }, partitionEntry: newPartition2);

        using DicomWebResponse<IEnumerable<PartitionEntry>> response3 = await _client.GetPartitionsAsync();
        Assert.True(response3.IsSuccessStatusCode);

        IEnumerable<PartitionEntry> values = await response3.GetValueAsync();

        Assert.Contains(values, x => x.PartitionName == newPartition1.PartitionName);
        Assert.Contains(values, x => x.PartitionName == newPartition2.PartitionName);
    }

    [Fact]
    [Trait("Category", "bvt-dp")]
    public async Task GivenDatasetWithNewPartitionName_WhenStoring_TheServerShouldReturnWithNewPartition()
    {
        var newPartition = new PartitionEntry(DefaultPartition.Key, TestUidGenerator.Generate());

        string studyInstanceUID = TestUidGenerator.Generate();

        DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID);

        using DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(new[] { dicomFile }, partitionEntry: newPartition);

        Assert.True(response.IsSuccessStatusCode);

        ValidationHelpers.ValidateReferencedSopSequence(
            await response.GetValueAsync(),
            ConvertToReferencedSopSequenceEntry(dicomFile.Dataset, newPartition.PartitionName));
    }

    [Fact]
    [Trait("Category", "bvt-dp")]
    public async Task GivenDatasetWithNewPartitionName_WhenStoringWithStudyUid_TheServerShouldReturnWithNewPartition()
    {
        var newPartition = new PartitionEntry(DefaultPartition.Key, TestUidGenerator.Generate());

        var studyInstanceUID = TestUidGenerator.Generate();
        DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUid: studyInstanceUID);

        using DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(dicomFile, studyInstanceUID, newPartition);

        Assert.True(response.IsSuccessStatusCode);

        ValidationHelpers.ValidateReferencedSopSequence(
            await response.GetValueAsync(),
            ConvertToReferencedSopSequenceEntry(dicomFile.Dataset, newPartition.PartitionName));
    }

    [Fact]
    [Trait("Category", "bvt-dp")]
    public async Task WhenRetrievingWithPartitionName_TheServerShouldReturnOnlyTheSpecifiedPartition()
    {
        var newPartition1 = new PartitionEntry(DefaultPartition.Key, TestUidGenerator.Generate());
        var newPartition2 = new PartitionEntry(DefaultPartition.Key, TestUidGenerator.Generate());

        string studyInstanceUID = TestUidGenerator.Generate();
        string seriesInstanceUID = TestUidGenerator.Generate();
        string sopInstanceUID = TestUidGenerator.Generate();

        DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID, seriesInstanceUID, sopInstanceUID);

        using DicomWebResponse<DicomDataset> response1 = await _instancesManager.StoreAsync(new[] { dicomFile }, partitionEntry: newPartition1);
        using DicomWebResponse<DicomDataset> response2 = await _instancesManager.StoreAsync(new[] { dicomFile }, partitionEntry: newPartition2);

        using DicomWebResponse<DicomFile> response3 = await _client.RetrieveInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, partitionName: newPartition1.PartitionName);
        Assert.True(response3.IsSuccessStatusCode);

        using DicomWebResponse<DicomFile> response4 = await _client.RetrieveInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, partitionName: newPartition2.PartitionName);
        Assert.True(response4.IsSuccessStatusCode);
    }

    [Fact]
    [Trait("Category", "bvt-dp")]
    public async Task WhenRendereingWithPartitionName_TheServerShouldReturnOnlyTheSpecifiedPartition()
    {
        var newPartition1 = new PartitionEntry(DefaultPartition.Key, TestUidGenerator.Generate());
        var newPartition2 = new PartitionEntry(DefaultPartition.Key, TestUidGenerator.Generate());

        string studyInstanceUID = TestUidGenerator.Generate();
        string seriesInstanceUID = TestUidGenerator.Generate();
        string sopInstanceUID = TestUidGenerator.Generate();

        DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUID, seriesInstanceUID, sopInstanceUID, frames: 3);

        using DicomWebResponse<DicomDataset> response1 = await _instancesManager.StoreAsync(new[] { dicomFile }, partitionEntry: newPartition1);
        using DicomWebResponse<DicomDataset> response2 = await _instancesManager.StoreAsync(new[] { dicomFile }, partitionEntry: newPartition2);

        using DicomWebResponse<Stream> response3 = await _client.RetrieveRenderedInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, partitionName: newPartition1.PartitionName);
        Assert.True(response3.IsSuccessStatusCode);

        using DicomWebResponse<Stream> response4 = await _client.RetrieveRenderedInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, partitionName: newPartition2.PartitionName);
        Assert.True(response4.IsSuccessStatusCode);
    }

    [Fact]
    [Trait("Category", "bvt-dp")]
    public async Task GivenDatasetInstancesWithDifferentPartitions_WhenDeleted_OneDeletedAndOtherRemains()
    {
        var newPartition1 = new PartitionEntry(DefaultPartition.Key, TestUidGenerator.Generate());
        var newPartition2 = new PartitionEntry(DefaultPartition.Key, TestUidGenerator.Generate());

        string studyInstanceUID = TestUidGenerator.Generate();
        string seriesInstanceUID = TestUidGenerator.Generate();
        string sopInstanceUID = TestUidGenerator.Generate();

        DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID, seriesInstanceUID, sopInstanceUID);

        using DicomWebResponse<DicomDataset> response1 = await _instancesManager.StoreAsync(new[] { dicomFile }, partitionEntry: newPartition1);
        using DicomWebResponse<DicomDataset> response2 = await _instancesManager.StoreAsync(new[] { dicomFile }, partitionEntry: newPartition2);

        using DicomWebResponse response3 = await _client.DeleteInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, newPartition1.PartitionName);
        Assert.True(response3.IsSuccessStatusCode);

        await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, partitionName: newPartition1.PartitionName));

        using DicomWebResponse<DicomFile> response5 = await _client.RetrieveInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, partitionName: newPartition2.PartitionName);
        Assert.True(response5.IsSuccessStatusCode);
    }

    [Fact]
    [Trait("Category", "bvt-dp")]
    public async Task GivenMatchingStudiesInDifferentPartitions_WhenSearchForStudySeriesLevel_OnePartitionMatchesResult()
    {
        var newPartition1 = new PartitionEntry(DefaultPartition.Key, TestUidGenerator.Generate());
        var newPartition2 = new PartitionEntry(DefaultPartition.Key, TestUidGenerator.Generate());

        var studyUid = TestUidGenerator.Generate();

        DicomFile file1 = Samples.CreateRandomDicomFile(studyUid);
        file1.Dataset.AddOrUpdate(new DicomDataset()
        {
             { DicomTag.Modality, "MRI" },
        });

        DicomFile file2 = Samples.CreateRandomDicomFile(studyUid);
        file2.Dataset.AddOrUpdate(new DicomDataset()
        {
             { DicomTag.Modality, "MRI" },
        });

        using DicomWebResponse<DicomDataset> response1 = await _instancesManager.StoreAsync(new[] { file1 }, partitionEntry: newPartition1);
        using DicomWebResponse<DicomDataset> response2 = await _instancesManager.StoreAsync(new[] { file2 }, partitionEntry: newPartition2);

        using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryStudySeriesAsync(studyUid, "Modality=MRI", newPartition1.PartitionName);

        DicomDataset[] datasets = await response.ToArrayAsync();

        Assert.Single(datasets);
        ValidationHelpers.ValidateResponseDataset(QueryResource.StudySeries, file1.Dataset, datasets[0]);
    }

    [Fact]
    [Trait("Category", "bvt-dp")]
    public async Task GivenAnInstance_WhenRetrievingChangeFeedWithPartition_ThenPartitionNameIsReturned()
    {
        var newPartition = new PartitionEntry(DefaultPartition.Key, TestUidGenerator.Generate());
        string studyInstanceUID = TestUidGenerator.Generate();
        string seriesInstanceUID = TestUidGenerator.Generate();
        string sopInstanceUID = TestUidGenerator.Generate();

        DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID, seriesInstanceUID, sopInstanceUID);
        using DicomWebResponse<DicomDataset> response1 = await _instancesManager.StoreAsync(new[] { dicomFile }, partitionEntry: newPartition);
        Assert.True(response1.IsSuccessStatusCode);

        long initialSequence;

        using (DicomWebResponse<ChangeFeedEntry> response = await _client.GetChangeFeedLatest("?includemetadata=false"))
        {
            ChangeFeedEntry changeFeedEntry = await response.GetValueAsync();

            initialSequence = changeFeedEntry.Sequence;

            Assert.Equal(newPartition.PartitionName, changeFeedEntry.PartitionName);
            Assert.Equal(studyInstanceUID, changeFeedEntry.StudyInstanceUid);
            Assert.Equal(seriesInstanceUID, changeFeedEntry.SeriesInstanceUid);
            Assert.Equal(sopInstanceUID, changeFeedEntry.SopInstanceUid);
        }

        using (DicomWebAsyncEnumerableResponse<ChangeFeedEntry> response = await _client.GetChangeFeed($"?offset={initialSequence - 1}&includemetadata=false"))
        {
            ChangeFeedEntry[] changeFeedResults = await response.ToArrayAsync();

            Assert.Single(changeFeedResults);
            Assert.Null(changeFeedResults[0].Metadata);
            Assert.Equal(newPartition.PartitionName, changeFeedResults[0].PartitionName);
            Assert.Equal(ChangeFeedState.Current, changeFeedResults[0].State);
        }
    }

    [Fact]
    [Trait("Category", "bvt-dp")]
    public async Task WhenAddingWorkitem_TheServerShouldCreateWorkitemSuccessfully()
    {
        DicomDataset dicomDataset = Samples.CreateRandomWorkitemInstanceDataset();
        var workitemUid = dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

        using DicomWebResponse response = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid, TestUidGenerator.Generate());

        Assert.True(response.IsSuccessStatusCode);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _instancesManager.DisposeAsync();
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
