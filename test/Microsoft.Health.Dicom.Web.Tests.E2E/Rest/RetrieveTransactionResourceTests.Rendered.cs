// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Net;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

/// <summary>
/// <inheritdoc/>
/// </summary>
public partial class RetrieveTransactionResourceTests
{
    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    public async Task GivenValidMediaType_WhenRetrieveRenderedInstance_ThenServerShouldReturnCorrectContentSuccessfully(string mediaType)
    {
        string studyInstanceUID = TestUidGenerator.Generate();
        string seriesInstanceUID = TestUidGenerator.Generate();
        string sopInstanceUID = TestUidGenerator.Generate();

        DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUID, seriesInstanceUID, sopInstanceUID);

        using DicomWebResponse<DicomDataset> response1 = await _instancesManager.StoreAsync(new[] { dicomFile });

        using DicomWebResponse<Stream> response2 = await _client.RetrieveRenderedInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, 100, mediaType);
        Assert.True(response2.IsSuccessStatusCode);
        Assert.Equal(mediaType, response2.ContentHeaders.ContentType.MediaType);

    }

    [Fact]
    public async Task GivenNoMediaType_WhenRetrieveRenderedFrame_ThenServerShouldReturnCorrectContentSuccessfully()
    {
        string studyInstanceUID = TestUidGenerator.Generate();
        string seriesInstanceUID = TestUidGenerator.Generate();
        string sopInstanceUID = TestUidGenerator.Generate();

        DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUID, seriesInstanceUID, sopInstanceUID, frames: 3);

        using DicomWebResponse<DicomDataset> response1 = await _instancesManager.StoreAsync(new[] { dicomFile });

        using DicomWebResponse<Stream> response2 = await _client.RetrieveRenderedFrameAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, 1);
        Assert.True(response2.IsSuccessStatusCode);
        Assert.Equal("image/jpeg", response2.ContentHeaders.ContentType.MediaType);

        using DicomWebResponse<Stream> response3 = await _client.RetrieveRenderedFrameAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, 2);
        Assert.True(response3.IsSuccessStatusCode);
        Assert.Equal("image/jpeg", response2.ContentHeaders.ContentType.MediaType);
    }

    [Fact]
    public async Task GivenNoMediaType_WhenRetrieveInvalidRenderedFrame_ThenServerShouldThrowError()
    {
        string studyInstanceUID = TestUidGenerator.Generate();
        string seriesInstanceUID = TestUidGenerator.Generate();
        string sopInstanceUID = TestUidGenerator.Generate();

        DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUID, seriesInstanceUID, sopInstanceUID, frames: 3);

        using DicomWebResponse<DicomDataset> response1 = await _instancesManager.StoreAsync(new[] { dicomFile });

        using DicomWebResponse<Stream> response2 = await _client.RetrieveRenderedFrameAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, 1);
        Assert.True(response2.IsSuccessStatusCode);
        Assert.Equal("image/jpeg", response2.ContentHeaders.ContentType.MediaType);

        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveRenderedFrameAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, 5));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GivenDifferentQualities_WhenRetrieveRenderedFrame_ThenServerShouldReturnCorrectContentSuccessfully()
    {
        string studyInstanceUID = TestUidGenerator.Generate();
        string seriesInstanceUID = TestUidGenerator.Generate();
        string sopInstanceUID = TestUidGenerator.Generate();

        DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUID, seriesInstanceUID, sopInstanceUID, frames: 3);

        using DicomWebResponse<DicomDataset> response1 = await _instancesManager.StoreAsync(new[] { dicomFile });

        using DicomWebResponse<Stream> response2 = await _client.RetrieveRenderedFrameAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, 1, 100);
        Assert.True(response2.IsSuccessStatusCode);
        Assert.Equal("image/jpeg", response2.ContentHeaders.ContentType.MediaType);

        using DicomWebResponse<Stream> response3 = await _client.RetrieveRenderedFrameAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, 2, 100);
        Assert.True(response3.IsSuccessStatusCode);
        Assert.Equal("image/jpeg", response2.ContentHeaders.ContentType.MediaType);

        using DicomWebResponse<Stream> response4 = await _client.RetrieveRenderedFrameAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, 1, 1);
        Assert.True(response4.IsSuccessStatusCode);
        Assert.Equal("image/jpeg", response4.ContentHeaders.ContentType.MediaType);

        using DicomWebResponse<Stream> response5 = await _client.RetrieveRenderedFrameAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, 2, 1);
        Assert.True(response5.IsSuccessStatusCode);
        Assert.Equal("image/jpeg", response5.ContentHeaders.ContentType.MediaType);
    }
}
