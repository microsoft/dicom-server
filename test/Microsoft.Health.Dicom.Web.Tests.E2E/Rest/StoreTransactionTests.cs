// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Http;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Comparers;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public abstract class StoreTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>, IAsyncLifetime
{
    private protected readonly IDicomWebClient _client;
    private protected readonly DicomInstancesManager _instancesManager;
    private protected readonly RecyclableMemoryStreamManager _pool = new RecyclableMemoryStreamManager();
    private protected readonly string _partition = TestUidGenerator.Generate();

    protected StoreTransactionTests(HttpIntegrationTestFixture<Startup> fixture)
    {
        _client = GetClient(EnsureArg.IsNotNull(fixture, nameof(fixture)));
        _instancesManager = new DicomInstancesManager(_client);
        DicomValidationBuilderExtension.SkipValidation(null);
    }

    protected abstract IDicomWebClient GetClient(HttpIntegrationTestFixture<Startup> fixture);

    [Fact]
    public async Task GivenEmptyContent_WhenStoring_TheServerShouldReturnConflict()
    {
        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(Stream.Null));
        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
    }

    [Fact]
    public async Task GivenARequestWithInvalidStudyInstanceUID_WhenStoring_TheServerShouldReturnBadRequest()
    {
        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(new[] { Stream.Null }, studyInstanceUid: new string('b', 65)));
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    [Theory]
    [MemberData(nameof(GetIncorrectAcceptHeaders))]
    public async Task GivenAnIncorrectAcceptHeader_WhenStoring_TheServerShouldReturnNotAcceptable(string acceptHeader)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(DicomApiVersions.Latest + DicomWebConstants.StudiesUriString, UriKind.Relative));
        request.Headers.Add(HeaderNames.Accept, acceptHeader);

        using HttpResponseMessage response = await _client.HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
    }

    [Fact]
    public async Task GivenAnNonMultipartRequest_WhenStoring_TheServerShouldReturnUnsupportedMediaType()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(DicomApiVersions.Latest + DicomWebConstants.StudiesUriString, UriKind.Relative));
        request.Headers.Add(HeaderNames.Accept, DicomWebConstants.MediaTypeApplicationDicomJson.MediaType);

        using var multiContent = new MultipartContent("form");
        multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));
        request.Content = multiContent;

        using HttpResponseMessage response = await _client.HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task GivenAMultipartRequestWithNoContent_WhenStoring_TheServerShouldReturnNoContent()
    {
        using var multiContent = new MultipartContent("related");
        multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

        using DicomWebResponse response = await _client.StoreAsync(multiContent);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GivenAMultipartRequestWithEmptyContent_WhenStoring_TheServerShouldReturnConflict()
    {
        using var multiContent = new MultipartContent("related");
        multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

        using var byteContent = new ByteArrayContent(Array.Empty<byte>());
        byteContent.Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicom;
        multiContent.Add(byteContent);

        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(multiContent));
        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
    }

    [Fact]
    public async Task GivenAMultipartRequestWithAnInvalidMultipartSection_WhenStoring_TheServerShouldReturnAccepted()
    {
        using var multiContent = new MultipartContent("related");
        multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

        using var byteContent = new ByteArrayContent(Array.Empty<byte>());
        byteContent.Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicom;
        multiContent.Add(byteContent);

        string studyInstanceUID = TestUidGenerator.Generate();

        try
        {
            DicomFile validFile = Samples.CreateRandomDicomFile(studyInstanceUID);

            using DicomContent validContent = new(validFile);
            validContent.Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicom;
            multiContent.Add(validContent);

            using DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(multiContent, instanceId: DicomInstanceId.FromDicomFile(validFile));

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            await ResponseHelper.ValidateReferencedSopSequenceAsync(
                response,
                ResponseHelper.ConvertToReferencedSopSequenceEntry(_client, validFile.Dataset));
        }
        finally
        {
            await _client.DeleteStudyAsync(studyInstanceUID);
        }
    }

    [Fact]
    public async Task GivenAMultipartRequestWithTypeParameterAndFirstSectionWithoutContentType_WhenStoring_TheServerShouldReturnOK()
    {
        using var multiContent = new MultipartContent("related");
        multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

        string studyInstanceUID = TestUidGenerator.Generate();

        try
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID);

            using DicomContent content = new(dicomFile);
            content.Headers.ContentType = null;
            multiContent.Add(content);

            using DicomWebResponse<DicomDataset> response = await _instancesManager
                .StoreAsync(multiContent, instanceId: DicomInstanceId.FromDicomFile(dicomFile));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await ResponseHelper.ValidateReferencedSopSequenceAsync(
                response,
                ResponseHelper.ConvertToReferencedSopSequenceEntry(_client, dicomFile.Dataset));
        }
        finally
        {
            await _client.DeleteStudyAsync(studyInstanceUID);
        }
    }

    [Fact]
    public async Task GivenAllDifferentStudyInstanceUIDs_WhenStoringWithProvidedStudyInstanceUID_TheServerShouldReturnConflict()
    {
        DicomFile dicomFile1 = Samples.CreateRandomDicomFile();
        DicomFile dicomFile2 = Samples.CreateRandomDicomFile();

        var studyInstanceUID = TestUidGenerator.Generate();

        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
            () => _client.StoreAsync(new[] { dicomFile1, dicomFile2 }, studyInstanceUID));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);

        DicomDataset dataset = exception.ResponseDataset;

        Assert.NotNull(dataset);
        Assert.True(dataset.Count() == 1);

        ValidationHelpers.ValidateFailedSopSequence(
            dataset,
            ResponseHelper.ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, ValidationHelpers.ValidationFailedFailureCode),
            ResponseHelper.ConvertToFailedSopSequenceEntry(dicomFile2.Dataset, ValidationHelpers.ValidationFailedFailureCode));
    }

    [Fact]
    [Trait("Category", "bvt")]
    public async Task GivenOneDifferentStudyInstanceUID_WhenStoringWithProvidedStudyInstanceUID_TheServerShouldReturnAccepted()
    {
        var studyInstanceUID1 = TestUidGenerator.Generate();
        var studyInstanceUID2 = TestUidGenerator.Generate();

        try
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid: studyInstanceUID1);
            DicomFile dicomFile2 = Samples.CreateRandomDicomFile(studyInstanceUid: studyInstanceUID2);

            using DicomWebResponse<DicomDataset> response = await _client.StoreAsync(new[] { dicomFile1, dicomFile2 }, studyInstanceUID1);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            DicomInstanceId missing = DicomInstanceId.FromDicomFile(dicomFile2);
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                () => _client.RetrieveInstanceMetadataAsync(
                    missing.StudyInstanceUid,
                    missing.SeriesInstanceUid,
                    missing.SopInstanceUid));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);

            DicomDataset dataset = await response.GetValueAsync();

            Assert.NotNull(dataset);
            Assert.Equal(3, dataset.Count());

            Assert.EndsWith($"studies/{studyInstanceUID1}", dataset.GetSingleValue<string>(DicomTag.RetrieveURL));

            await ResponseHelper.ValidateReferencedSopSequenceAsync(
                response,
                ResponseHelper.ConvertToReferencedSopSequenceEntry(_client, dicomFile1.Dataset));

            ValidationHelpers.ValidateFailedSopSequence(
                dataset,
                ResponseHelper.ConvertToFailedSopSequenceEntry(dicomFile2.Dataset, ValidationHelpers.ValidationFailedFailureCode));
        }
        finally
        {
            await _client.DeleteStudyAsync(studyInstanceUID1);
        }
    }

    [Fact]
    public async Task GivenDatasetWithDuplicateIdentifiers_WhenStoring_TheServerShouldReturnAccepted()
    {
        var studyInstanceUID = TestUidGenerator.Generate();
        DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID, studyInstanceUID);

        using DicomWebResponse<DicomDataset> response = await _client.StoreAsync(new[] { dicomFile1, dicomFile1 }, studyInstanceUID);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task GivenExistingDataset_WhenStoring_TheServerShouldReturnConflict()
    {
        var studyInstanceUID = TestUidGenerator.Generate();

        try
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID);

            using DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(dicomFile1);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            DicomDataset dataset = await response.GetValueAsync();
            await ResponseHelper.ValidateReferencedSopSequenceAsync(
                response,
                ResponseHelper.ConvertToReferencedSopSequenceEntry(
                    _client,
                    dicomFile1.Dataset));
            Assert.False(dataset.TryGetSequence(DicomTag.FailedSOPSequence, out DicomSequence _));

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(dicomFile1));
            Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);

            ValidationHelpers.ValidateFailedSopSequence(
                exception.ResponseDataset,
                ResponseHelper.ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, ValidationHelpers.SopInstanceAlreadyExistsFailureCode));
        }
        finally
        {
            await _client.DeleteStudyAsync(studyInstanceUID);
        }
    }

    [Theory]
    [InlineData("abc.123")]
    [InlineData("11|")]
    public async Task GivenDatasetWithInvalidUid_WhenStoring_TheServerShouldReturnConflict(string studyInstanceUID)
    {
        DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID, validateItems: false);

        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(dicomFile1));
        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);

        Assert.False(exception.ResponseDataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence _));

        ValidationHelpers.ValidateFailedSopSequence(
            exception.ResponseDataset,
            ResponseHelper.ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, ValidationHelpers.ValidationFailedFailureCode));
    }

    [Fact]
    [Trait("Category", "bvt")]
    public async Task GivenSinglePartRequest_WhenStoring_ThenServerShouldReturnOK()
    {
        DicomFile dicomFile = Samples.CreateRandomDicomFile();
        using DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(dicomFile);

        Assert.Equal(KnownContentTypes.ApplicationDicomJson, response.ContentHeaders.ContentType.MediaType);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "bvt")]
    public async Task GivenSinglePartRequest_WhenStoring_ThenShouldRetrieveEquivalentBytes()
    {
        const int Pixels = 7240;
        DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(
            rows: Pixels,
            columns: Pixels,
            dicomTransferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian); // ~50MB

        using DicomWebResponse<DicomDataset> stow = await _instancesManager.StoreAsync(dicomFile);
        Assert.Equal(HttpStatusCode.OK, stow.StatusCode);

        InstanceIdentifier id = dicomFile.Dataset.ToInstanceIdentifier(Partition.Default);
        using DicomWebResponse<DicomFile> wado = await _client.RetrieveInstanceAsync(
            id.StudyInstanceUid,
            id.SeriesInstanceUid,
            id.SopInstanceUid,
            dicomTransferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

        // Compare Streams
        using MemoryStream before = _pool.GetStream();
        await dicomFile.SaveAsync(before);
        before.Seek(0, SeekOrigin.Begin);

        Assert.True(before.Length > Pixels * Pixels);
        Assert.Equal(before, await wado.Content.ReadAsStreamAsync(), BinaryComparer.Instance);
    }

    [Fact]
    public async Task GivenSinglePartRequest_WhenStoringWithStudyUid_ThenServerShouldReturnOK()
    {
        var studyInstanceUID = TestUidGenerator.Generate();
        DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUid: studyInstanceUID);

        using DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(dicomFile);
        Assert.Equal(KnownContentTypes.ApplicationDicomJson, response.ContentHeaders.ContentType.MediaType);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GivenDicomTagWithMultipleValues_WhenStoring_ThenShouldSucceedWithWarning()
    {
        DicomDataset dataset = Samples.CreateRandomInstanceDataset().NotValidated();
        DicomLongString studyDescription = new DicomLongString(DicomTag.StudyDescription, "Value1", "Value2");
        dataset.AddOrUpdate(studyDescription);

        var response = await _instancesManager.StoreAsync(new DicomFile(dataset));

        // TODO:  Verify warning content after https://microsofthealth.visualstudio.com/Health/_workitems/edit/91168 is fixed.
    }

    [Fact]
    [Trait("Category", "bvt")]
    public async Task GivenMultiPartRequest_WhenStoring_ThenShouldRetrieveEquivalentBytes()
    {
        const int Pixels = 4000;
        string studyInstanceUid = TestUidGenerator.Generate();
        DicomFile[] files = Enumerable
            .Repeat(studyInstanceUid, 3)
            .Select(study => Samples.CreateRandomDicomFileWithPixelData(
                studyInstanceUid: study,
                rows: Pixels,
                columns: Pixels,
                dicomTransferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian)) // ~15MB
            .ToArray();

        using DicomWebResponse<DicomDataset> stow = await _instancesManager.StoreAsync(files);
        Assert.Equal(HttpStatusCode.OK, stow.StatusCode);

        // Validate content using WADO
        foreach (DicomFile expected in files)
        {
            InstanceIdentifier id = expected.Dataset.ToInstanceIdentifier(Partition.Default);
            using DicomWebResponse<DicomFile> wado = await _client.RetrieveInstanceAsync(
                id.StudyInstanceUid,
                id.SeriesInstanceUid,
                id.SopInstanceUid,
                dicomTransferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            using MemoryStream before = _pool.GetStream();
            await expected.SaveAsync(before);
            before.Seek(0, SeekOrigin.Begin);

            Assert.True(before.Length > Pixels * Pixels);
            Assert.Equal(before, await wado.Content.ReadAsStreamAsync(), BinaryComparer.Instance);
        }
    }

    /*
     * A customer is sending us UIDs with a trailing space. This is invalid, but may be due to their interpretation of
     * the padding requirement to add a null character to make length even.
     * See https://dicom.nema.org/dicom/2013/output/chtml/part05/chapter_9.html
     *
     * Sql Server will automatically pad strings for whitespace when comparing. See
     * https://support.microsoft.com/en-us/topic/inf-how-sql-server-compares-strings-with-trailing-spaces-b62b1a2d-27d3-4260-216d-a605719003b0
     *
     * This test ensures that
     * - when a user saves their file with StudyInstanceUID contianing whitespace, we allow it and save it with whitespace
     * - when a user tries to save the same file with different number in whitespace padding, they receive a conflict
     */
    [Fact]
    public async Task GivenInstanceWithPaddedStudyInstanceUIDAlreadyStored_WhenStoreInstanceWithMorePaddedId_ThenExpectConflict()
    {
        var studyInstanceUid = TestUidGenerator.Generate() + " ";
        var seriesInstanceUid = TestUidGenerator.Generate();
        var sopInstanceUid = TestUidGenerator.Generate();

        // store first time
        DicomFile dicomFile1 = Samples.CreateRandomDicomFile(
            studyInstanceUid: studyInstanceUid,
            seriesInstanceUid: seriesInstanceUid,
            sopInstanceUid: sopInstanceUid);
        await _instancesManager.StoreAsync(dicomFile1);

        // retrieve and assert file stored with padded whitespace
        using DicomWebResponse<DicomFile> instanceRetrieve = await _client.RetrieveInstanceAsync(
            studyInstanceUid,
            seriesInstanceUid,
            sopInstanceUid,
            dicomTransferSyntax: "*");

        DicomFile retrievedDicomFile = await instanceRetrieve.GetValueAsync();
        Assert.Equal(
            studyInstanceUid,
            retrievedDicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID));

        // store again, with additional padding on studyInstanceUid
        dicomFile1.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, studyInstanceUid + "     ");
        var ex = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(dicomFile1));
        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task GivenInstanceWithImplicitVRPrivateTag_WhenStored_ThePrivateTagShouldBeRemovedFromMetadata()
    {
        // setup
        DicomTag privateTag = new DicomTag(0007, 0008);
        DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(dicomTransferSyntax: DicomTransferSyntax.ImplicitVRLittleEndian);
        dicomFile.Dataset.Add(DicomVR.LO, privateTag, "Private Tag");

        await _instancesManager.StoreAsync(dicomFile);
        using DicomWebResponse<DicomFile> retrievedInstance = await _client.RetrieveInstanceAsync(
            dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
            dicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
            dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID));

        DicomFile retrievedDicomFile = await retrievedInstance.GetValueAsync();

        // The private tag should exist because we do not change the file itself
        Assert.True(retrievedDicomFile.Dataset.Contains(privateTag));

        using DicomWebAsyncEnumerableResponse<DicomDataset> retrievedInstanceMetadata = await _client.RetrieveInstanceMetadataAsync(
            dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
            dicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
            dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID));

        DicomDataset metadata = await retrievedInstanceMetadata.SingleAsync();

        // The private tag should be removed from the metadata since VR is unknown
        Assert.False(metadata.Contains(privateTag));
    }

    public static IEnumerable<object[]> GetIncorrectAcceptHeaders
    {
        get
        {
            yield return new object[] { "application/dicom" };
            yield return new object[] { "application/data" };
        }
    }
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _instancesManager.DisposeAsync();
    }

    private protected static DicomFile GenerateDicomFile()
    {
        DicomFile dicomFile = Samples.CreateRandomDicomFile(
            studyInstanceUid: TestUidGenerator.Generate(),
            seriesInstanceUid: TestUidGenerator.Generate(),
            sopInstanceUid: TestUidGenerator.Generate()
        );
        return dicomFile;
    }

    private protected async Task<IEnumerable<DicomDataset>> GetInstanceByAttribute(DicomFile dicomFile, DicomTag searchTag)
    {
        using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryInstancesAsync(
            $"{searchTag.DictionaryEntry.Keyword}={dicomFile.Dataset.GetString(searchTag)}");
        Assert.Equal(KnownContentTypes.ApplicationDicomJson, response.ContentHeaders.ContentType.MediaType);
        DicomDataset[] datasets = await response.ToArrayAsync();

        IEnumerable<DicomDataset> matchedInstances = datasets.Where(
            ds => ds.GetString(DicomTag.StudyInstanceUID) == dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID));
        return matchedInstances;
    }
}
