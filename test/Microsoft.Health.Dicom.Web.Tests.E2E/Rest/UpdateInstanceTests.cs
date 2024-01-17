// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Comparers;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Microsoft.Health.Operations;
using Xunit;
using FunctionsStartup = Microsoft.Health.Dicom.Functions.App.Startup;
using WebStartup = Microsoft.Health.Dicom.Web.Startup;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

[Trait("Category", "bvt")]
[Collection("Update Collection")]
public class UpdateInstanceTests : IClassFixture<WebJobsIntegrationTestFixture<WebStartup, FunctionsStartup>>, IAsyncLifetime
{
    private readonly IDicomWebClient _client;
    private readonly IDicomWebClient _v1Client;
    private readonly DicomTagsManager _tagManager;
    private readonly DicomInstancesManager _instancesManager;

    public UpdateInstanceTests(WebJobsIntegrationTestFixture<WebStartup, FunctionsStartup> fixture)
    {
        EnsureArg.IsNotNull(fixture, nameof(fixture));
        _client = fixture.GetDicomWebClient();
        _v1Client = fixture.GetDicomWebClient(DicomApiVersions.V1);
        _tagManager = new DicomTagsManager(_client);
        _instancesManager = new DicomInstancesManager(_client);
    }

    [Fact]
    public async Task GivenV1DicomClient_WhenUpdateStudy_TheItShouldReturnNotFound()
    {
        string studyInstanceUid1 = TestUidGenerator.Generate();
        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _v1Client.UpdateStudyAsync(new[] { studyInstanceUid1 }, new DicomDataset()));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task WhenUpdatingDicomMetadataForASingleStudy_ThenItShouldUpdateCorrectly()
    {
        string studyInstanceUid = TestUidGenerator.Generate();

        DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid);
        DicomFile dicomFile2 = Samples.CreateRandomDicomFile(studyInstanceUid);
        DicomFile dicomFile3 = Samples.CreateRandomDicomFile(studyInstanceUid);

        // Upload files
        Assert.True((await _instancesManager.StoreStudyAsync(new[] { dicomFile1, dicomFile2, dicomFile3 })).IsSuccessStatusCode);

        // Update study
        await UpdateStudyAsync(expectedInstancesUpdated: 3, expectedStudyUpdated: 1, studyInstanceUid, "New^PatientName");

        // Verify study
        await VerifyMetadata(studyInstanceUid, Enumerable.Repeat("New^PatientName", 3).ToArray());
    }

    [Fact]
    public async Task WhenUpdatingForAUnknownStudy_ThenItShouldCompleteOperationSuccessfully()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string studyInstanceUid1 = TestUidGenerator.Generate();

        DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid);

        // Upload files
        Assert.True((await _instancesManager.StoreStudyAsync(new[] { dicomFile1 })).IsSuccessStatusCode);

        // Update study
        await UpdateStudyAsync(expectedInstancesUpdated: 0, expectedStudyUpdated: 0, studyInstanceUid1, "New^PatientName");
    }

    [Fact]
    public async Task WhenUpdatingDicomMetadataForStudyWithMultipleInstances_ThenItShouldUpdateCorrectly()
    {
        string studyInstanceUid1 = TestUidGenerator.Generate();
        string studyInstanceUid2 = TestUidGenerator.Generate();

        DicomFile dicomFile1 = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUid1, rows: 200, columns: 200, frames: 10, dicomTransferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian);
        DicomFile dicomFile2 = Samples.CreateRandomDicomFile(studyInstanceUid1);
        DicomFile dicomFile3 = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUid2);
        string originalPatientName1 = dicomFile1.Dataset.GetSingleValue<string>(DicomTag.PatientName);
        string originalPatientName2 = dicomFile2.Dataset.GetSingleValue<string>(DicomTag.PatientName);

        // Upload files
        Assert.True((await _instancesManager.StoreAsync(new[] { dicomFile1, dicomFile2, dicomFile3 })).IsSuccessStatusCode);

        // Update study
        await UpdateStudyAsync(expectedInstancesUpdated: 2, expectedStudyUpdated: 1, studyInstanceUid1, "New^PatientName");

        // Verify study
        await VerifyMetadata(studyInstanceUid1, Enumerable.Repeat("New^PatientName", 2).ToArray());
        await VerifyRetrieveInstance(studyInstanceUid1, dicomFile1, "New^PatientName", true);

        // Update again to ensure DICOM file is not corrupted after update
        await UpdateStudyAsync(expectedInstancesUpdated: 2, expectedStudyUpdated: 1, studyInstanceUid1, "New^PatientName1");

        // Verify again to ensure update is successful
        await VerifyRetrieveInstance(studyInstanceUid1, dicomFile1, "New^PatientName1", true);
        await VerifyRetrieveInstanceWithTranscoding(studyInstanceUid1, dicomFile1, "New^PatientName1", true);
        await VerifyMetadata(studyInstanceUid1, new string[] { originalPatientName1, originalPatientName2 }, true);
        await VerifyRetrieveFrame(studyInstanceUid1, dicomFile1);
    }

    [Fact]
    public async Task WhenUpdatingDicomMetadataWithDicomFileSizeGreaterThan4MB_ThenItShouldUpdateCorrectly()
    {
        string studyInstanceUid1 = TestUidGenerator.Generate();

        DicomFile dicomFile1 = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUid1, rows: 1000, columns: 1000, frames: 5, dicomTransferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian);
        string originalPatientName1 = dicomFile1.Dataset.GetSingleValue<string>(DicomTag.PatientName);

        // Upload files
        Assert.True((await _instancesManager.StoreAsync(new[] { dicomFile1 })).IsSuccessStatusCode);

        // Update study
        await UpdateStudyAsync(expectedInstancesUpdated: 1, expectedStudyUpdated: 1, studyInstanceUid1, "New^PatientName");

        // Verify study
        await VerifyMetadata(studyInstanceUid1, Enumerable.Repeat("New^PatientName", 1).ToArray());
        await VerifyRetrieveInstance(studyInstanceUid1, dicomFile1, "New^PatientName", true);
        await VerifyRetrieveFrame(studyInstanceUid1, dicomFile1);

        // Update again to ensure DICOM file is not corrupted after update
        await UpdateStudyAsync(expectedInstancesUpdated: 1, expectedStudyUpdated: 1, studyInstanceUid1, "New^PatientName1");

        // Verify again to ensure update is successful
        await VerifyRetrieveInstance(studyInstanceUid1, dicomFile1, "New^PatientName1", true);
        await VerifyRetrieveInstanceWithTranscoding(studyInstanceUid1, dicomFile1, "New^PatientName1", true);
        await VerifyMetadata(studyInstanceUid1, new string[] { originalPatientName1 }, true);
        await VerifyRetrieveFrame(studyInstanceUid1, dicomFile1);
    }

    [Fact]
    public async Task GivenInstanceUpdated_WhenDeleting_ThenItShouldDeleteBothOriginalAndNew()
    {
        string studyInstanceUid1 = TestUidGenerator.Generate();
        DicomFile dicomFile1 = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUid1, rows: 200, columns: 200, frames: 10, dicomTransferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian);

        // Upload original file
        Assert.True((await _instancesManager.StoreAsync(new[] { dicomFile1 })).IsSuccessStatusCode);

        // Update study
        await UpdateStudyAsync(expectedInstancesUpdated: 1, expectedStudyUpdated: 1, studyInstanceUid1, "New^PatientName");

        // call delete service and verify both new and original blobs deleted
        await VerifyDeleteStudyAsync(studyInstanceUid1, dicomFile1, requestOriginalVersion: true);
    }

    [Fact]
    public async Task WhenUpdatingDicomMetadataWithExtendedQueryTagForASingleStudy_ThenItShouldUpdateCorrectly()
    {
        DicomTag ageTag = DicomTag.PatientAge;
        DicomTag patientSexTag = DicomTag.PatientSex;
        string tagValue = "035Y";

        // Try to delete these extended query tags.
        await _tagManager.DeleteExtendedQueryTagAsync(ageTag.GetPath());
        await _tagManager.DeleteExtendedQueryTagAsync(patientSexTag.GetPath());

        string studyInstanceUid = TestUidGenerator.Generate();

        DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid);
        dicomFile1.Dataset.Add(ageTag, tagValue);

        DicomFile dicomFile2 = Samples.CreateRandomDicomFile(studyInstanceUid);
        dicomFile2.Dataset.Add(ageTag, tagValue);

        DicomFile dicomFile3 = Samples.CreateRandomDicomFile(studyInstanceUid);
        dicomFile3.Dataset.Add(ageTag, tagValue);

        // Upload files
        Assert.True((await _instancesManager.StoreStudyAsync(new[] { dicomFile1, dicomFile2, dicomFile3 })).IsSuccessStatusCode);

        // Add extended query tag
        Assert.Equal(
            OperationStatus.Succeeded,
            await _tagManager.AddTagsAsync(
                new AddExtendedQueryTagEntry { Path = ageTag.GetPath(), VR = ageTag.GetDefaultVR().Code, Level = QueryTagLevel.Study },
                new AddExtendedQueryTagEntry { Path = patientSexTag.GetPath(), VR = patientSexTag.GetDefaultVR().Code, Level = QueryTagLevel.Study }));

        // Update study
        await UpdateStudyAsync(expectedInstancesUpdated: 3, expectedStudyUpdated: 1, studyInstanceUid, "New^PatientName", "054Y", "M", expectedPhysicianName: "NewPhysicianName");

        // Verify using QIDO
        DicomWebAsyncEnumerableResponse<DicomDataset> queryResponse = await _client.QueryInstancesAsync($"{ageTag.GetPath()}=054Y&{patientSexTag.GetPath()}=M&{DicomTag.ReferringPhysicianName.GetPath()}=NewPhysicianName");
        DicomDataset[] instances = await queryResponse.ToArrayAsync();
        Assert.Equal(3, instances.Length);

        // Verify using QIDO not original values
        queryResponse = await _client.QueryInstancesAsync($"{ageTag.GetPath()}=035Y&{DicomTag.StudyInstanceUID.GetPath()}={studyInstanceUid}");
        instances = await queryResponse.ToArrayAsync();
        Assert.Empty(instances);
    }

    private async Task UpdateStudyAsync(
        int expectedInstancesUpdated,
        int expectedStudyUpdated,
        string studyInstanceUid,
        string expectedPatientName,
        string age = null,
        string patientSex = null,
        string expectedPhysicianName = null)
    {
        var datasetToUpdate = new DicomDataset();
        datasetToUpdate.AddOrUpdate(DicomTag.PatientName, expectedPatientName);

        if (!string.IsNullOrEmpty(age))
        {
            datasetToUpdate.AddOrUpdate(DicomTag.PatientAge, age);
        }

        if (!string.IsNullOrEmpty(patientSex))
        {
            datasetToUpdate.AddOrUpdate(DicomTag.PatientSex, patientSex);
        }

        if (!string.IsNullOrEmpty(expectedPhysicianName))
        {
            datasetToUpdate.AddOrUpdate(DicomTag.ReferringPhysicianName, expectedPhysicianName);
        }

        IOperationState<DicomOperation> response = await _instancesManager.UpdateStudyAsync([studyInstanceUid], datasetToUpdate);

        Assert.Equal(OperationStatus.Succeeded, response.Status);

        var updateResult = response.Results as UpdateResults;
        Assert.NotNull(updateResult);
        Assert.Equal(expectedInstancesUpdated, updateResult.InstanceUpdated);
        Assert.Equal(0, updateResult.StudyFailed);
        Assert.Equal(expectedStudyUpdated, updateResult.StudyUpdated);
        Assert.Equal(1, updateResult.StudyProcessed);
        Assert.Null(updateResult.Errors);
    }

    private async Task VerifyRetrieveInstance(string studyInstanceUid, DicomFile dicomFile, string expectedPatientName, bool requestOriginalVersion = default)
    {
        using DicomWebResponse<DicomFile> instanceRetrieve = await _client.RetrieveInstanceAsync(
            studyInstanceUid,
            dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID),
            dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID),
            dicomTransferSyntax: "*");

        DicomFile updatedFile = await instanceRetrieve.GetValueAsync();

        Assert.Equal(expectedPatientName, updatedFile.Dataset.GetSingleValue<string>(DicomTag.PatientName));

        if (requestOriginalVersion)
        {
            using DicomWebResponse<DicomFile> instanceRetrieve1 = await _client.RetrieveInstanceAsync(
                studyInstanceUid,
                dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID),
                dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID),
                dicomTransferSyntax: "*",
                requestOriginalVersion: true);

            DicomFile originalFile = await instanceRetrieve1.GetValueAsync();
            Assert.NotNull(originalFile);

            VerifyPixelData(originalFile, updatedFile);
        }
    }

    private async Task VerifyMetadata(string studyInstanceUid, string[] expectedPatientNames, bool requestOriginalVersion = default)
    {
        // Verify study
        using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.RetrieveStudyMetadataAsync(studyInstanceUid, requestOriginalVersion: requestOriginalVersion);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/dicom+json", response.ContentHeaders.ContentType.MediaType);

        DicomDataset[] datasets = await response.ToArrayAsync();

        Assert.Equal(expectedPatientNames.Length, datasets.Length);
        string[] actualPatientNames = datasets.Select(x => x.GetSingleValue<string>(DicomTag.PatientName)).ToArray();

        Assert.True(expectedPatientNames.All(x => actualPatientNames.Contains(x)));
    }

    private async Task VerifyRetrieveFrame(string studyInstanceUid, DicomFile dicomFile)
    {
        int numberOfFrames = dicomFile.Dataset.GetSingleValue<int>(DicomTag.NumberOfFrames);
        var pixelData = DicomPixelData.Create(dicomFile.Dataset);
        for (int i = 0; i < numberOfFrames; i++)
        {
            using DicomWebResponse<Stream> response = await _client.RetrieveSingleFrameAsync(
               studyInstanceUid,
               dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID),
               dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID),
               i + 1);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using Stream frameStream = await response.GetValueAsync();
            Assert.NotNull(frameStream);
            Assert.Equal(frameStream.ToByteArray(), pixelData.GetFrame(i).Data, BinaryComparer.Instance);
        }
    }

    private async Task VerifyRetrieveInstanceWithTranscoding(string studyInstanceUid, DicomFile dicomFile, string expectedPatientName, bool requestOriginalVersion = default)
    {
        using DicomWebResponse<DicomFile> instanceRetrieve = await _client.RetrieveInstanceAsync(
            studyInstanceUid,
            dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID),
            dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID),
            dicomTransferSyntax: DicomTransferSyntax.JPEG2000Lossless.UID.UID);

        DicomFile retrievedDicomFile = await instanceRetrieve.GetValueAsync();

        Assert.Equal(expectedPatientName, retrievedDicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientName));

        if (requestOriginalVersion)
        {
            using DicomWebResponse<DicomFile> instanceRetrieve1 = await _client.RetrieveInstanceAsync(
                studyInstanceUid,
                dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID),
                dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID),
                dicomTransferSyntax: DicomTransferSyntax.JPEG2000Lossless.UID.UID,
                requestOriginalVersion: true);
            DicomFile retrievedDicomFile1 = await instanceRetrieve1.GetValueAsync();
            Assert.NotNull(retrievedDicomFile1);
        }
    }

    private async Task VerifyDeleteStudyAsync(string studyInstanceUid, DicomFile dicomFile, bool requestOriginalVersion = default)
    {
        // When deleted an instance that has been updated, both new and original files must be deleted
        var seriesInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
        var sopInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

        using (DicomWebResponse response = await _client.DeleteStudyAsync(studyInstanceUid))
        {
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        DicomWebException exception1 = await Assert.ThrowsAsync<DicomWebException>(
            () => _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUID, sopInstanceUID));
        Assert.Equal(HttpStatusCode.NotFound, exception1.StatusCode);

        DicomWebException exception2 = await Assert.ThrowsAsync<DicomWebException>(
            () => _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUID, sopInstanceUID, requestOriginalVersion: requestOriginalVersion));
        Assert.Equal(HttpStatusCode.NotFound, exception2.StatusCode);

        await Assert.ThrowsAsync<DicomWebException>(
            () => _client.RetrieveStudyMetadataAsync(studyInstanceUid, requestOriginalVersion: requestOriginalVersion));
        await Assert.ThrowsAsync<DicomWebException>(
            () => _client.RetrieveStudyMetadataAsync(studyInstanceUid));
    }

    private static void VerifyPixelData(DicomFile originalFile, DicomFile updateFile)
    {
        var originalPixelData = DicomPixelData.Create(originalFile.Dataset);
        var updatePixelData = DicomPixelData.Create(updateFile.Dataset);

        Assert.Equal(originalPixelData.NumberOfFrames, updatePixelData.NumberOfFrames);

        for (int i = 0; i < originalPixelData.NumberOfFrames; i++)
        {
            Assert.Equal(originalPixelData.GetFrame(i).Data, updatePixelData.GetFrame(i).Data, BinaryComparer.Instance);
        }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _instancesManager.DisposeAsync();
    }
}
