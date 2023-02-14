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
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public class StoreTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>, IAsyncLifetime
{
    private readonly IDicomWebClient _client;
    private readonly IDicomWebClient _clientV2;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly DicomInstancesManager _instancesManager;
    private readonly DicomInstancesManager _instancesManagerV2;

    public StoreTransactionTests(HttpIntegrationTestFixture<Startup> fixture)
    {
        EnsureArg.IsNotNull(fixture, nameof(fixture));
        _client = fixture.GetDicomWebClient();
        _clientV2 = fixture.GetDicomWebClient(DicomApiVersions.V2);
        _instancesManager = new DicomInstancesManager(_client);
        _instancesManagerV2 = new DicomInstancesManager(_clientV2);
        _recyclableMemoryStreamManager = fixture.RecyclableMemoryStreamManager;
    }

    [Fact]
    public async Task GivenV2NotEnabled_WhenAttemptingToUseV2_TheExpectUnsupportedApiVersionExceptionThrown()
    {
        var studyInstanceUID1 = TestUidGenerator.Generate();
        DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid: studyInstanceUID1);

        DicomWebException exception =
            await Assert.ThrowsAsync<DicomWebException>(() => _clientV2.StoreAsync(dicomFile1));

        Assert.Contains(
            """BadRequest: {"error":{"code":"UnsupportedApiVersion""",
            exception.Message);
    }

    [Fact]
    public async Task GivenRandomContent_WhenStoring_TheServerShouldReturnConflict()
    {
        await using MemoryStream stream = _recyclableMemoryStreamManager.GetStream();

        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(new[] { stream }));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
    }

    [Fact]
    public async Task GivenARequestWithInvalidStudyInstanceUID_WhenStoring_TheServerShouldReturnBadRequest()
    {
        await using MemoryStream stream = _recyclableMemoryStreamManager.GetStream();

        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(new[] { stream }, studyInstanceUid: new string('b', 65)));

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

        var multiContent = new MultipartContent("form");
        multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));
        request.Content = multiContent;

        using HttpResponseMessage response = await _client.HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task GivenAMultipartRequestWithNoContent_WhenStoring_TheServerShouldReturnNoContent()
    {
        var multiContent = new MultipartContent("related");
        multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

        using DicomWebResponse response = await _instancesManager.StoreAsync(multiContent);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GivenAMultipartRequestWithEmptyContent_WhenStoring_TheServerShouldReturnConflict()
    {
        var multiContent = new MultipartContent("related");
        multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

        var byteContent = new ByteArrayContent(Array.Empty<byte>());
        byteContent.Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicom;
        multiContent.Add(byteContent);

        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
            () => _instancesManager.StoreAsync(multiContent));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
    }

    [Fact]
    public async Task GivenAMultipartRequestWithAnInvalidMultipartSection_WhenStoring_TheServerShouldReturnAccepted()
    {
        var multiContent = new MultipartContent("related");
        multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

        var byteContent = new ByteArrayContent(Array.Empty<byte>());
        byteContent.Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicom;
        multiContent.Add(byteContent);

        string studyInstanceUID = TestUidGenerator.Generate();

        try
        {
            DicomFile validFile = Samples.CreateRandomDicomFile(studyInstanceUID);

            await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
            {
                await validFile.SaveAsync(stream);

                var validByteContent = new ByteArrayContent(stream.ToArray());
                validByteContent.Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicom;
                multiContent.Add(validByteContent);
            }

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
        var multiContent = new MultipartContent("related");
        multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

        string studyInstanceUID = TestUidGenerator.Generate();

        try
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID);

            await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
            {
                await dicomFile.SaveAsync(stream);

                var byteContent = new ByteArrayContent(stream.ToArray());
                multiContent.Add(byteContent);
            }

            using DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(multiContent, instanceId: DicomInstanceId.FromDicomFile(dicomFile));

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

        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _instancesManager.StoreAsync(
            new[] { dicomFile1, dicomFile2 }, studyInstanceUid: studyInstanceUID));

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

            using DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(new[] { dicomFile1, dicomFile2 }, studyInstanceUid: studyInstanceUID1);

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            DicomDataset dataset = await response.GetValueAsync();

            Assert.NotNull(dataset);
            Assert.True(dataset.Count() == 3);

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
    public async Task GivenDatasetWithDuplicateIdentifiers_WhenStoring_TheServerShouldReturnConflict()
    {
        var studyInstanceUID = TestUidGenerator.Generate();
        DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID, studyInstanceUID);

        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _instancesManager.StoreAsync(new[] { dicomFile1 }));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);

        DicomDataset dataset = exception.ResponseDataset;

        Assert.False(dataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence _));

        ValidationHelpers.ValidateFailedSopSequence(
            dataset,
            ResponseHelper.ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, ValidationHelpers.ValidationFailedFailureCode));
    }

    [Fact]
    public async Task GivenExistingDataset_WhenStoring_TheServerShouldReturnConflict()
    {
        var studyInstanceUID = TestUidGenerator.Generate();

        try
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID);

            using DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(new[] { dicomFile1 });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            DicomDataset dataset = await response.GetValueAsync();

            await ResponseHelper.ValidateReferencedSopSequenceAsync(
                response,
                ResponseHelper.ConvertToReferencedSopSequenceEntry(
                    _client,
                    dicomFile1.Dataset));

            Assert.False(dataset.TryGetSequence(DicomTag.FailedSOPSequence, out DicomSequence _));

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                () => _instancesManager.StoreAsync(new[] { dicomFile1 }));

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

    [Fact]
    public async Task GivenDatasetWithInvalidVrValue_WhenStoring_TheServerShouldReturnConflict()
    {
        var studyInstanceUID = TestUidGenerator.Generate();

        DicomFile dicomFile1 = Samples.CreateRandomDicomFileWithInvalidVr(studyInstanceUID);

        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _instancesManager.StoreAsync(new[] { dicomFile1 }));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.False(exception.ResponseDataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence _));

        ValidationHelpers.ValidateFailedSopSequence(
            exception.ResponseDataset,
            ResponseHelper.ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, ValidationHelpers.ValidationFailedFailureCode));
    }

    [Theory]
    [InlineData("abc.123")]
    [InlineData("11|")]
    public async Task GivenDatasetWithInvalidUid_WhenStoring_TheServerShouldReturnConflict(string studyInstanceUID)
    {
        DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID, validateItems: false);

        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _instancesManager.StoreAsync(new[] { dicomFile1 }));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);

        Assert.False(exception.ResponseDataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence _));

        ValidationHelpers.ValidateFailedSopSequence(
            exception.ResponseDataset,
            ResponseHelper.ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, ValidationHelpers.ValidationFailedFailureCode));
    }

    [Fact]
    [Trait("Category", "bvt")]
    public async Task StoreSinglepart_ServerShouldReturnOK()
    {
        DicomFile dicomFile = Samples.CreateRandomDicomFile();

        await using MemoryStream stream = _recyclableMemoryStreamManager.GetStream();
        await dicomFile.SaveAsync(stream);

        using DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(stream, instanceId: DicomInstanceId.FromDicomFile(dicomFile));
        Assert.Equal(KnownContentTypes.ApplicationDicomJson, response.ContentHeaders.ContentType.MediaType);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StoreSinglepartWithStudyUID_ServerShouldReturnOK()
    {
        var studyInstanceUID = TestUidGenerator.Generate();
        DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUid: studyInstanceUID);

        using DicomWebResponse<DicomDataset> response = await _instancesManager.StoreAsync(dicomFile, studyInstanceUid: studyInstanceUID);
        Assert.Equal(KnownContentTypes.ApplicationDicomJson, response.ContentHeaders.ContentType.MediaType);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GivenDicomTagWithMultipleValues_WhenStoring_ThenShouldSucceeWithWarning()
    {
        DicomDataset dataset = Samples.CreateRandomInstanceDataset().NotValidated();
        DicomLongString studyDescription = new DicomLongString(DicomTag.StudyDescription, "Value1", "Value2");
        dataset.AddOrUpdate(studyDescription);

        var response = await _instancesManager.StoreAsync(new DicomFile(dataset));

        // TODO:  Verify warning content after https://microsofthealth.visualstudio.com/Health/_workitems/edit/91168 is fixed.
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
        await _instancesManager.StoreAsync(new[] { dicomFile1 });

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
        var ex = await Assert.ThrowsAsync<DicomWebException>(() => _instancesManager.StoreAsync(new[] { dicomFile1 }));
        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
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
}
