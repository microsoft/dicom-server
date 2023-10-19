// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Comparers;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Dicom.Tests.Common.TranscoderTests;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

/// <summary>
/// <inheritdoc/>
/// </summary>
public partial class RetrieveTransactionResourceTests
{
    [Theory]
    [InlineData(RequestOriginalContentTestFolder, "*")]
    [InlineData(FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder, null)]
    [InlineData(FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder, "1.2.840.10008.1.2.1")]
    [InlineData(FromExplicitVRLittleEndianToJPEG2000LosslessTestFolder, "1.2.840.10008.1.2.4.90")]
    public async Task GivenSinglePartAcceptHeader_WhenRetrieveInstance_ThenServerShouldReturnExpectedContent(string testDataFolder, string transferSyntax)
    {
        TranscoderTestData transcoderTestData = TranscoderTestDataHelper.GetTestData(Path.Combine(TestFileFolder, testDataFolder));
        DicomFile inputDicomFile = DicomFile.Open(transcoderTestData.InputDicomFile);
        var instanceId = RandomizeInstanceIdentifier(inputDicomFile.Dataset);

        await _instancesManager.StoreAsync(inputDicomFile);

        using DicomWebResponse<DicomFile> response = await _client.RetrieveInstanceAsync(instanceId.StudyInstanceUid, instanceId.SeriesInstanceUid, instanceId.SopInstanceUid, transferSyntax);

        Assert.Equal(DicomWebConstants.ApplicationDicomMediaType, response.ContentHeaders.ContentType.MediaType);

        var actual = await response.GetValueAsync();
        var expected = DicomFile.Open(transcoderTestData.ExpectedOutputDicomFile);
        Assert.Equal(expected, actual, new DicomFileEqualityComparer(_ignoredSet));
    }

    [Theory]
    [MemberData(nameof(GetAcceptHeadersForInstances))]
    public async Task GivenMultipartAcceptHeader_WhenRetrieveInstance_ThenServerShouldReturnExpectedContent(string testDataFolder, string transferSyntax)
    {
        TranscoderTestData transcoderTestData = TranscoderTestDataHelper.GetTestData(testDataFolder);
        DicomFile inputDicomFile = DicomFile.Open(transcoderTestData.InputDicomFile);
        var instanceId = RandomizeInstanceIdentifier(inputDicomFile.Dataset);

        await _instancesManager.StoreAsync(new[] { inputDicomFile });

        var requestUri = new Uri(DicomApiVersions.Latest + string.Format(CultureInfo.InvariantCulture, DicomWebConstants.BaseInstanceUriFormat, instanceId.StudyInstanceUid, instanceId.SeriesInstanceUid, instanceId.SopInstanceUid), UriKind.Relative);

        using HttpRequestMessage request = HttpRequestMessageBuilder.Build(requestUri, singlePart: false, DicomWebConstants.ApplicationDicomMediaType, transferSyntax);
        using HttpResponseMessage response = await _client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(GeUnsupportedAcceptHeadersForInstances))]
    public async Task GivenUnsupportedAcceptHeaders_WhenRetrieveInstance_ThenServerShouldReturnNotAcceptable(bool singlePart, string mediaType, string transferSyntax)
    {
        var requestUri = new Uri(DicomApiVersions.Latest + string.Format(CultureInfo.InvariantCulture, DicomWebConstants.BaseInstanceUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate()), UriKind.Relative);

        using HttpRequestMessage request = HttpRequestMessageBuilder.Build(requestUri, singlePart: singlePart, mediaType, transferSyntax);
        using HttpResponseMessage response = await _client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
    }

    [Fact]
    public async Task GivenUnsupportedInternalTransferSyntax_WhenRetrieveInstance_ThenServerShouldReturnNotAcceptable()
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var seriesInstanceUid = TestUidGenerator.Generate();
        var sopInstanceUid = TestUidGenerator.Generate();
        DicomFile dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
            studyInstanceUid,
            seriesInstanceUid,
            sopInstanceUid,
            transferSyntax: DicomTransferSyntax.MPEG2.UID.UID,
            encode: false);

        await _instancesManager.StoreAsync(new[] { dicomFile });
        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTransferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID));
        Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
    }

    [Fact]
    [Trait("Category", "bvt")]
    public async Task GivenUnsupportedInternalTransferSyntax_WhenRetrieveInstanceWithOriginalTransferSyntax_ThenServerShouldReturnOriginalContent()
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var seriesInstanceUid = TestUidGenerator.Generate();
        var sopInstanceUid = TestUidGenerator.Generate();

        DicomFile dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
            studyInstanceUid,
            seriesInstanceUid,
            sopInstanceUid,
            transferSyntax: DicomTransferSyntax.MPEG2.UID.UID,
            encode: false);

        await _instancesManager.StoreAsync(new[] { dicomFile });

        using DicomWebResponse<DicomFile> instancesInStudy = await _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTransferSyntax: "*");
        Assert.Equal(dicomFile.ToByteArray(), (await instancesInStudy.GetValueAsync()).ToByteArray());
    }

    [Fact]
    public async Task GivenNonExistingInstance_WhenRetrieveInstance_ThenServerShouldReturnNotFound()
    {
        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate()));
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GivenInstanceWithoutPixelData_WhenRetrieveInstance_ThenServerShouldReturnExpectedContent()
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var seriesInstanceUid = TestUidGenerator.Generate();
        var sopInstanceUid = TestUidGenerator.Generate();
        DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
        await _instancesManager.StoreAsync(new[] { dicomFile1 });

        using DicomWebResponse<DicomFile> instanceRetrieve = await _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTransferSyntax: "*");
        Assert.Equal(dicomFile1.ToByteArray(), (await instanceRetrieve.GetValueAsync()).ToByteArray());
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
     * - when a user retrieves that saved file, the StudyInstanceUID comes back with whitespace
     * - when a user retrieves that saved file, they can do so with or without padding StudyInstanceUID in their query param
     */
    [Theory]
    [InlineData(" ", " ")]
    [InlineData("", " ")]
    [InlineData(" ", "")]
    [InlineData("     ", " ")]
    public async Task GivenInstanceWithPaddedStudyInstanceUID_WhenRetrieveInstance_ThenServerShouldReturnExpectedContent(
        string queryStudyInstanceUidPadding,
        string saveStudyInstanceUidPadding)
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var queryStudyInstanceUid = studyInstanceUid + queryStudyInstanceUidPadding;
        var saveStudyInstanceUid = studyInstanceUid + saveStudyInstanceUidPadding;


        var seriesInstanceUid = TestUidGenerator.Generate();
        var sopInstanceUid = TestUidGenerator.Generate();

        DicomFile dicomFile1 = Samples.CreateRandomDicomFile(
            studyInstanceUid: saveStudyInstanceUid,
            seriesInstanceUid: seriesInstanceUid,
            sopInstanceUid: sopInstanceUid);
        await _instancesManager.StoreAsync(new[] { dicomFile1 });

        using DicomWebResponse<DicomFile> instanceRetrieve = await _client.RetrieveInstanceAsync(
            queryStudyInstanceUid,
            seriesInstanceUid,
            sopInstanceUid,
            dicomTransferSyntax: "*");

        DicomFile retrievedDicomFile = await instanceRetrieve.GetValueAsync();

        Assert.Equal(
            dicomFile1.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID),
            retrievedDicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID));
        Assert.Equal(
            queryStudyInstanceUid.TrimEnd(),
            retrievedDicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID).TrimEnd());
    }

    /*
     * This test ensures existing DICOM instances/metadata/frames are retreivable.
     * Since our PR environment, creates new db for each test run, the tests is not so usable there
     * but it will make sure the existing instances are retreivable in our CI environment.
     * Once we move the blob file path to sql db, we can remove this test.
     */
    [Fact]
    [Trait("Category", "bvt")]
    public async Task GivenExistingInstance_WhenRetrievingInstanceAndMetadataAndFrame_ThenServerShouldReturnExpectedContent()
    {
        string studyInstanceUid = "2.25.81807007645997311198377430799026916602";
        string seriesInstanceUid = "2.25.250753924732793947834686260446683374093";
        string sopInstanceUid = "2.25.107991042123998946672148230267983649341";

        DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
        await _instancesManager.StoreIfNotExistsAsync(dicomFile, doNotDelete: true);

        using DicomWebResponse<DicomFile> instanceRetrieve = await _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTransferSyntax: "*");
        Assert.Equal(HttpStatusCode.OK, instanceRetrieve.StatusCode);
        Assert.NotNull((await instanceRetrieve.GetValueAsync()));

        using DicomWebAsyncEnumerableResponse<DicomDataset> metadataResponse = await _client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
        Assert.Equal(HttpStatusCode.OK, metadataResponse.StatusCode);
        DicomDataset[] datasets = await metadataResponse.ToArrayAsync();
        Assert.NotNull(datasets.First());

        using DicomWebResponse<Stream> frameResponse = await _client.RetrieveSingleFrameAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, 1);
        Assert.Equal(HttpStatusCode.OK, frameResponse.StatusCode);
        using Stream frameStream = await frameResponse.GetValueAsync();
        Assert.NotNull(frameStream);
    }

    public static IEnumerable<object[]> GetAcceptHeadersForInstances
    {
        get
        {
            yield return new object[] { Path.Combine(TestFileFolder, RequestOriginalContentTestFolder), "*" };
            yield return new object[] { Path.Combine(TestFileFolder, FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder), null };
            yield return new object[] { Path.Combine(TestFileFolder, FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder), "1.2.840.10008.1.2.1" };
        }
    }

    public static IEnumerable<object[]> GeUnsupportedAcceptHeadersForInstances
    {
        get
        {
            yield return new object[] { true, DicomWebConstants.ApplicationOctetStreamMediaType, DicomWebConstants.OriginalDicomTransferSyntax }; // unsupported media type image/png
            yield return new object[] { true, DicomWebConstants.ApplicationDicomMediaType, "1.2.840.10008.1.2.4.100" }; // unsupported transfer syntax MPEG2
        }
    }
}
