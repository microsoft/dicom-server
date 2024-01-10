// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Web;
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
    [InlineData(FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder, DicomWebConstants.ApplicationOctetStreamMediaType, null)]
    [InlineData(FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder, DicomWebConstants.ApplicationOctetStreamMediaType, "1.2.840.10008.1.2.1")]
    [InlineData(RequestOriginalContentTestFolder, DicomWebConstants.ApplicationOctetStreamMediaType, "*")]
    [InlineData(FromExplicitVRLittleEndianToJPEG2000LosslessTestFolder, DicomWebConstants.ImageJpeg2000MediaType, null)]
    [InlineData(FromExplicitVRLittleEndianToJPEG2000LosslessTestFolder, DicomWebConstants.ImageJpeg2000MediaType, "1.2.840.10008.1.2.4.90")]
    public async Task GivenSupportedAcceptHeaders_WhenRetrieveFrame_ThenServerShouldReturnExpectedContent(string testDataFolder, string mediaType, string transferSyntax)
    {
        TranscoderTestData transcoderTestData = TranscoderTestDataHelper.GetTestData(Path.Combine(TestFileFolder, testDataFolder));
        DicomFile inputDicomFile = await DicomFile.OpenAsync(transcoderTestData.InputDicomFile);
        var instanceId = RandomizeInstanceIdentifier(inputDicomFile.Dataset);

        await _instancesManager.StoreAsync(inputDicomFile);

        DicomFile outputDicomFile = DicomFile.Open(transcoderTestData.ExpectedOutputDicomFile);
        DicomPixelData pixelData = DicomPixelData.Create(outputDicomFile.Dataset);

        using DicomWebAsyncEnumerableResponse<Stream> response = await _client.RetrieveFramesAsync(
            instanceId.StudyInstanceUid,
            instanceId.SeriesInstanceUid,
            instanceId.SopInstanceUid,
            frames: new[] { 1 },
            mediaType,
            transferSyntax);

        int frameIndex = 0;

        await foreach (Stream item in response)
        {
            // TODO: verify media type once https://microsofthealth.visualstudio.com/Health/_workitems/edit/75185 is done
            Assert.Equal(item.ToByteArray(), pixelData.GetFrame(frameIndex).Data);
            frameIndex++;
        }
    }

    [Fact]
    public async Task GivenUnsupportedTransferSyntax_WhenRetrieveFrameWithOriginalTransferSyntax_ThenOriginalContentReturned()
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var seriesInstanceUid = TestUidGenerator.Generate();
        var sopInstanceUid = TestUidGenerator.Generate();

        DicomFile dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
            studyInstanceUid,
            seriesInstanceUid,
            sopInstanceUid,
            transferSyntax: DicomTransferSyntax.HEVCH265Main10ProfileLevel51.UID.UID,
            encode: false);

        await _instancesManager.StoreAsync(dicomFile);

        // Check for series
        using DicomWebAsyncEnumerableResponse<Stream> response = await _client.RetrieveFramesAsync(
            studyInstanceUid,
            seriesInstanceUid,
            sopInstanceUid,
            dicomTransferSyntax: "*",
            frames: new[] { 1 });

        Stream[] results = await response.ToArrayAsync();

        Assert.Collection(
            results,
            item => Assert.Equal(item.ToByteArray(), DicomPixelData.Create(dicomFile.Dataset).GetFrame(0).Data));
    }

    [Fact]
    public async Task GivenAnyMediaType_WhenRetrieveFrameWithOriginalTransferSyntax_ThenOriginalContentReturned()
    {
        var studyInstanceUid = TestUidGenerator.Generate();
        var seriesInstanceUid = TestUidGenerator.Generate();
        var sopInstanceUid = TestUidGenerator.Generate();

        DicomFile dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
            studyInstanceUid,
            seriesInstanceUid,
            sopInstanceUid,
            encode: false);

        await _instancesManager.StoreAsync(dicomFile);

        // Check for series
        using DicomWebAsyncEnumerableResponse<Stream> response = await _client.RetrieveFramesAsync(
            studyInstanceUid,
            seriesInstanceUid,
            sopInstanceUid,
            mediaType: "*/*",
            frames: new[] { 1 });

        Stream[] results = await response.ToArrayAsync();

        Assert.Collection(
            results,
            item => Assert.Equal(item.ToByteArray(), DicomPixelData.Create(dicomFile.Dataset).GetFrame(0).Data, BinaryComparer.Instance));
    }

    [Fact]
    public async Task GivenUnsupportedTransferSyntax_WhenRetrieveFrame_ThenServerShouldReturnNotAcceptable()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        DicomFile dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
            studyInstanceUid,
            seriesInstanceUid,
            sopInstanceUid,
            transferSyntax: DicomTransferSyntax.HEVCH265Main10ProfileLevel51.UID.UID,
            encode: false);

        await _instancesManager.StoreAsync(dicomFile);

        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveFramesAsync(
            studyInstanceUid,
            seriesInstanceUid,
            sopInstanceUid,
            dicomTransferSyntax: DicomTransferSyntax.JPEG2000Lossless.UID.UID,
            frames: new[] { 1 }));

        Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
    }

    [Fact]
    public async Task GivenMultipleFrames_WhenRetrieveFrame_ThenServerShouldReturnExpectedContent()
    {
        string studyInstanceUid = TestUidGenerator.Generate();

        DicomFile dicomFile1 = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUid, frames: 3);
        DicomPixelData pixelData = DicomPixelData.Create(dicomFile1.Dataset);
        InstanceIdentifier dicomInstance = dicomFile1.Dataset.ToInstanceIdentifier(Partition.Default);

        await _instancesManager.StoreAsync(dicomFile1);

        using DicomWebAsyncEnumerableResponse<Stream> response = await _client.RetrieveFramesAsync(
            dicomInstance.StudyInstanceUid,
            dicomInstance.SeriesInstanceUid,
            dicomInstance.SopInstanceUid,
            frames: new[] { 1, 2 },
            dicomTransferSyntax: "*");

        Stream[] frames = await response.ToArrayAsync();

        Assert.NotNull(frames);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, frames.Length);
        Assert.Equal(KnownContentTypes.MultipartRelated, response.ContentHeaders.ContentType.MediaType);
        Assert.Equal(pixelData.GetFrame(0).Data, frames[0].ToByteArray());
        Assert.Equal(pixelData.GetFrame(1).Data, frames[1].ToByteArray());
    }

    [Fact]
    public async Task GivenInstanceWithFrames_WhenRetrieveSinglePartOneFrame_ThenServerShouldReturnExpectedContent()
    {
        string studyInstanceUid = TestUidGenerator.Generate();

        DicomFile dicomFile1 = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUid, frames: 3);
        DicomPixelData pixelData = DicomPixelData.Create(dicomFile1.Dataset);
        InstanceIdentifier dicomInstance = dicomFile1.Dataset.ToInstanceIdentifier(Partition.Default);

        await _instancesManager.StoreAsync(dicomFile1);

        using DicomWebResponse<Stream> response = await _client.RetrieveSingleFrameAsync(
            dicomInstance.StudyInstanceUid,
            dicomInstance.SeriesInstanceUid,
            dicomInstance.SopInstanceUid,
            1);
        Stream frameStream = await response.GetValueAsync();
        Assert.NotNull(frameStream);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(KnownContentTypes.ApplicationOctetStream, response.ContentHeaders.ContentType.MediaType);
    }

    [Fact]
    public async Task GivenNonExistingFrames_WhenRetrieveFrame_ThenServerShouldReturnNotFound()
    {
        (InstanceIdentifier identifier, DicomFile file) = await CreateAndStoreDicomFile(2);

        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
            () => _client.RetrieveFramesAsync(identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid, dicomTransferSyntax: "*", frames: new[] { 4 }));
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1, -1)]
    [InlineData(0, 1)]
    public async Task GivenInvalidFrames_WhenRetrievingFrame_TheServerShouldReturnBadRequest(params int[] frames)
    {
        var requestUri = new Uri(string.Format(CultureInfo.InvariantCulture, DicomWebConstants.BaseRetrieveFramesUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), string.Join("%2C", frames)), UriKind.Relative);
        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
           () => _client.RetrieveFramesAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), frames));
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task GivenPathologyFile_WithOriginalGet_IsSuccessful()
    {
        string studyInstanceUid = "1.2.3.4.3";
        string seriesInstanceUid = "1.2.3.4.3.9423673";
        string sopInstanceUid = "1.3.6.1.4.1.45096.10.296485376.2210.1633373144.864450";

        try
        {
            using MemoryStream memoryStream = new MemoryStream(Resource.layer9);
            await _client.StoreAsync(memoryStream);
            await _client.RetrieveFramesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, new int[] { 1 });
        }
        finally
        {
            await _client.DeleteInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
        }
    }
}
