// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom.Imaging;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;
using NSubstitute;
using Xunit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.Health.Dicom.Core.Web;
using Xunit.Abstractions;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve;
public class RetrieveRenderedServiceTests
{
    private readonly RetrieveRenderedService _retrieveRenderedService;
    private readonly IInstanceStore _instanceStore;
    private readonly IFileStore _fileStore;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly ILogger<RetrieveRenderedService> _logger;
    private readonly string _studyInstanceUid = TestUidGenerator.Generate();
    private readonly string _firstSeriesInstanceUid = TestUidGenerator.Generate();
    private readonly string _sopInstanceUid = TestUidGenerator.Generate();
    private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;
    private static readonly FileProperties DefaultFileProperties = new FileProperties() { Path = "default/path/0.dcm", ETag = "123", ContentLength = 123 };
    private readonly RetrieveMeter _retrieveMeter;


    public RetrieveRenderedServiceTests(ITestOutputHelper output)
    {
        _instanceStore = Substitute.For<IInstanceStore>();
        _fileStore = Substitute.For<IFileStore>();
        _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        _dicomRequestContextAccessor.RequestContext.DataPartition = Partition.Default;
        var retrieveConfigurationSnapshot = Substitute.For<IOptionsSnapshot<RetrieveConfiguration>>();
        retrieveConfigurationSnapshot.Value.Returns(new RetrieveConfiguration());
        _retrieveMeter = new RetrieveMeter();

        _logger = output.ToLogger<RetrieveRenderedService>();

        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        _retrieveRenderedService = new RetrieveRenderedService(
            _instanceStore,
            _fileStore,
            _dicomRequestContextAccessor,
            retrieveConfigurationSnapshot,
            _recyclableMemoryStreamManager,
            _retrieveMeter,
            _logger
            );

        new DicomSetupBuilder()
        .RegisterServices(s => s.AddImageManager<ImageSharpImageManager>())
        .Build();
    }

    [Fact]
    public async Task GivenARequestWithMultipleAcceptHeaders_WhenServiceIsExecuted_ThenNotAcceptableExceptionExceptionIsThrown()
    {
        const string expectedErrorMessage = "The request contains multiple accept headers, which is not supported.";

        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Frames, 5, 75, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader(), AcceptHeaderHelpers.CreateRenderAcceptHeader() });
        var ex = await Assert.ThrowsAsync<NotAcceptableException>(() => _retrieveRenderedService.RetrieveRenderedImageAsync(request, CancellationToken.None));
        Assert.Equal(expectedErrorMessage, ex.Message);
    }

    [Fact]
    public async Task GivenARequestWithInvalidAcceptHeader_WhenServiceIsExecuted_ThenNotAcceptableExceptionExceptionIsThrown()
    {
        const string expectedErrorMessage = "The request headers are not acceptable";

        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Frames, 5, 75, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() });
        var ex = await Assert.ThrowsAsync<NotAcceptableException>(() => _retrieveRenderedService.RetrieveRenderedImageAsync(request, CancellationToken.None));
        Assert.Equal(expectedErrorMessage, ex.Message);
    }

    [Fact]
    public async Task GivenNoStoredInstances_RenderForInstance_ThenNotFoundIsThrown()
    {
        _instanceStore.GetInstanceIdentifiersInStudyAsync(Partition.Default, _studyInstanceUid).Returns(new List<VersionedInstanceIdentifier>());

        await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveRenderedService.RetrieveRenderedImageAsync(
            new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Instance, 1, 75, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader() }),
            DefaultCancellationToken));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(101)]
    public async Task GivenInvalidQuality_RenderForInstance_ThenBadRequestThrown(int quality)
    {
        const string expectedErrorMessage = "Image quality must be between 1 and 100 inclusive";

        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Frames, 5, quality, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader() });
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => _retrieveRenderedService.RetrieveRenderedImageAsync(request, CancellationToken.None));
        Assert.Equal(expectedErrorMessage, ex.Message);
    }

    [Fact]
    public async Task GivenFileSizeTooLarge_RenderForInstance_ThenNotFoundIsThrown()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(partition: _dicomRequestContextAccessor.RequestContext.DataPartition);
        long aboveMaxFileSize = new RetrieveConfiguration().MaxDicomFileSize + 1;
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Partition, DefaultFileProperties, DefaultCancellationToken).Returns(new FileProperties { ContentLength = aboveMaxFileSize });

        await Assert.ThrowsAsync<NotAcceptableException>(() =>
        _retrieveRenderedService.RetrieveRenderedImageAsync(
              new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Frames, 1, 75, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader() }),
              DefaultCancellationToken));

    }

    [Fact]
    public async Task GivenStoredInstancesWithFrames_WhenRenderRequestForNonExistingFrame_ThenNotFoundIsThrown()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(partition: _dicomRequestContextAccessor.RequestContext.DataPartition);

        // For the instance, set up the fileStore to return a stream containing the file associated with the identifier with 3 frames.
        Stream streamOfStoredFiles = (await RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier), _recyclableMemoryStreamManager, frames: 3)).Value;
        _fileStore.GetFileAsync(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Partition, DefaultFileProperties, DefaultCancellationToken).Returns(streamOfStoredFiles);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Partition, DefaultFileProperties, DefaultCancellationToken).Returns(new FileProperties { ContentLength = streamOfStoredFiles.Length });

        var retrieveRenderRequest = new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Frames, 5, 75, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader() });

        await Assert.ThrowsAsync<FrameNotFoundException>(() => _retrieveRenderedService.RetrieveRenderedImageAsync(
               retrieveRenderRequest,
               DefaultCancellationToken));

        // Dispose the stream.
        streamOfStoredFiles.Dispose();
    }

    [Fact]
    public async Task GivenStoredInstancesWithFramesJpeg_WhenRetrieveRenderedForFrames_ThenEachFrameRenderedSuccesfully()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList();

        KeyValuePair<DicomFile, Stream> streamAndStoredFile = await RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier), _recyclableMemoryStreamManager, frames: 3);
        _fileStore.GetFileAsync(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Partition, DefaultFileProperties, DefaultCancellationToken).Returns(streamAndStoredFile.Value);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Partition, DefaultFileProperties, DefaultCancellationToken).Returns(new FileProperties { ContentLength = streamAndStoredFile.Value.Length });

        MemoryStream copyStream = _recyclableMemoryStreamManager.GetStream();
        await streamAndStoredFile.Value.CopyToAsync(copyStream);
        copyStream.Position = 0;
        streamAndStoredFile.Value.Position = 0;

        MemoryStream streamAndStoredFileForFrame2 = _recyclableMemoryStreamManager.GetStream();
        await streamAndStoredFile.Value.CopyToAsync(streamAndStoredFileForFrame2);
        streamAndStoredFileForFrame2.Position = 0;
        streamAndStoredFile.Value.Position = 0;

        var retrieveRenderedRequest = new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Frames, 1, 75, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader() });

        RetrieveRenderedResponse response = await _retrieveRenderedService.RetrieveRenderedImageAsync(
               retrieveRenderedRequest,
               DefaultCancellationToken);

        DicomFile dicomFile = await DicomFile.OpenAsync(copyStream, FileReadOption.ReadLargeOnDemand);
        DicomImage dicomImage = new DicomImage(dicomFile.Dataset);
        using var img = dicomImage.RenderImage(0);
        using var sharpImage = img.AsSharpImage();
        using MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream();
        await sharpImage.SaveAsJpegAsync(resultStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder(), DefaultCancellationToken);
        resultStream.Position = 0;
        AssertStreamsEqual(resultStream, response.ResponseStream);
        Assert.Equal("image/jpeg", response.ContentType);
        Assert.Equal(1, _dicomRequestContextAccessor.RequestContext.PartCount);
        Assert.Equal(resultStream.Length, _dicomRequestContextAccessor.RequestContext.BytesRendered);

        var retrieveRenderedRequest2 = new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Frames, 2, 75, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader() });

        _fileStore.GetFileAsync(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Partition, DefaultFileProperties, DefaultCancellationToken).Returns(streamAndStoredFileForFrame2);
        RetrieveRenderedResponse response2 = await _retrieveRenderedService.RetrieveRenderedImageAsync(
               retrieveRenderedRequest2,
               DefaultCancellationToken);

        copyStream.Position = 0;
        using var img2 = dicomImage.RenderImage(1);
        using var sharpImage2 = img2.AsSharpImage();
        using MemoryStream resultStream2 = _recyclableMemoryStreamManager.GetStream();
        await sharpImage2.SaveAsJpegAsync(resultStream2, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder(), DefaultCancellationToken);
        resultStream2.Position = 0;
        AssertStreamsEqual(resultStream2, response2.ResponseStream);
        Assert.Equal("image/jpeg", response.ContentType);
        Assert.Equal(1, _dicomRequestContextAccessor.RequestContext.PartCount);
        Assert.Equal(resultStream2.Length, _dicomRequestContextAccessor.RequestContext.BytesRendered);

        copyStream.Dispose();
        streamAndStoredFileForFrame2.Dispose();
        response.ResponseStream.Dispose();
        response2.ResponseStream.Dispose();

    }

    [Fact]
    public async Task GivenStoredInstancesWithFramesJpeg_WhenRetrieveRenderedForFramesDifferentQuality_ThenEachFrameRenderedSuccesfully()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(partition: _dicomRequestContextAccessor.RequestContext.DataPartition);

        KeyValuePair<DicomFile, Stream> streamAndStoredFile = await RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier), _recyclableMemoryStreamManager, frames: 3);
        _fileStore.GetFileAsync(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Partition, DefaultFileProperties, DefaultCancellationToken).Returns(streamAndStoredFile.Value);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Partition, DefaultFileProperties, DefaultCancellationToken).Returns(new FileProperties { ContentLength = streamAndStoredFile.Value.Length });

        JpegEncoder jpegEncoder = new JpegEncoder();
        jpegEncoder.Quality = 50;
        streamAndStoredFile.Value.Position = 0;

        MemoryStream copyStream = _recyclableMemoryStreamManager.GetStream();
        await streamAndStoredFile.Value.CopyToAsync(copyStream);
        copyStream.Position = 0;
        streamAndStoredFile.Value.Position = 0;

        MemoryStream streamAndStoredFileForFrame2 = _recyclableMemoryStreamManager.GetStream();
        await streamAndStoredFile.Value.CopyToAsync(streamAndStoredFileForFrame2);
        streamAndStoredFileForFrame2.Position = 0;
        streamAndStoredFile.Value.Position = 0;

        var retrieveRenderedRequest = new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Frames, 1, 50, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader() });

        RetrieveRenderedResponse response = await _retrieveRenderedService.RetrieveRenderedImageAsync(
               retrieveRenderedRequest,
               DefaultCancellationToken);

        DicomFile dicomFile = await DicomFile.OpenAsync(copyStream, FileReadOption.ReadLargeOnDemand);
        DicomImage dicomImage = new DicomImage(dicomFile.Dataset);
        using var img = dicomImage.RenderImage(0);
        using var sharpImage = img.AsSharpImage();
        using MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream();
        await sharpImage.SaveAsJpegAsync(resultStream, jpegEncoder, DefaultCancellationToken);
        resultStream.Position = 0;
        AssertStreamsEqual(resultStream, response.ResponseStream);
        Assert.Equal("image/jpeg", response.ContentType);
        Assert.Equal(1, _dicomRequestContextAccessor.RequestContext.PartCount);
        Assert.Equal(resultStream.Length, _dicomRequestContextAccessor.RequestContext.BytesRendered);

        var retrieveRenderedRequest2 = new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Frames, 2, 20, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader() });

        _fileStore.GetFileAsync(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Partition, DefaultFileProperties, DefaultCancellationToken).Returns(streamAndStoredFileForFrame2);
        RetrieveRenderedResponse response2 = await _retrieveRenderedService.RetrieveRenderedImageAsync(
               retrieveRenderedRequest2,
               DefaultCancellationToken);

        copyStream.Position = 0;
        using var img2 = dicomImage.RenderImage(1);
        using var sharpImage2 = img2.AsSharpImage();
        using MemoryStream resultStream2 = _recyclableMemoryStreamManager.GetStream();
        jpegEncoder.Quality = 20;
        await sharpImage2.SaveAsJpegAsync(resultStream2, jpegEncoder, DefaultCancellationToken);
        resultStream2.Position = 0;
        AssertStreamsEqual(resultStream2, response2.ResponseStream);
        Assert.Equal("image/jpeg", response.ContentType);
        Assert.Equal(1, _dicomRequestContextAccessor.RequestContext.PartCount);
        Assert.Equal(resultStream2.Length, _dicomRequestContextAccessor.RequestContext.BytesRendered);

        copyStream.Dispose();
        streamAndStoredFileForFrame2.Dispose();
        response.ResponseStream.Dispose();
        response2.ResponseStream.Dispose();
    }

    [Fact]
    public async Task GivenStoredInstancesWithFramesPNG_WhenRetrieveRenderedForFrames_ThenEachFrameRenderedSuccesfully()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(partition: _dicomRequestContextAccessor.RequestContext.DataPartition);

        KeyValuePair<DicomFile, Stream> streamAndStoredFile = await RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier), _recyclableMemoryStreamManager, frames: 3);
        _fileStore.GetFileAsync(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Partition, DefaultFileProperties, DefaultCancellationToken).Returns(streamAndStoredFile.Value);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Partition, DefaultFileProperties, DefaultCancellationToken).Returns(new FileProperties { ContentLength = streamAndStoredFile.Value.Length });

        MemoryStream copyStream = _recyclableMemoryStreamManager.GetStream();
        await streamAndStoredFile.Value.CopyToAsync(copyStream);
        copyStream.Position = 0;
        streamAndStoredFile.Value.Position = 0;

        MemoryStream streamAndStoredFileForFrame2 = _recyclableMemoryStreamManager.GetStream();
        await streamAndStoredFile.Value.CopyToAsync(streamAndStoredFileForFrame2);
        streamAndStoredFileForFrame2.Position = 0;
        streamAndStoredFile.Value.Position = 0;

        var retrieveRenderedRequest = new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Frames, 1, 75, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader(mediaType: KnownContentTypes.ImagePng) });

        RetrieveRenderedResponse response = await _retrieveRenderedService.RetrieveRenderedImageAsync(
               retrieveRenderedRequest,
               DefaultCancellationToken);

        DicomFile dicomFile = await DicomFile.OpenAsync(copyStream, FileReadOption.ReadLargeOnDemand);
        DicomImage dicomImage = new DicomImage(dicomFile.Dataset);
        using var img = dicomImage.RenderImage(0);
        using var sharpImage = img.AsSharpImage();
        using MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream();
        await sharpImage.SaveAsPngAsync(resultStream, new SixLabors.ImageSharp.Formats.Png.PngEncoder(), DefaultCancellationToken);
        resultStream.Position = 0;
        AssertStreamsEqual(resultStream, response.ResponseStream);
        Assert.Equal("image/png", response.ContentType);
        Assert.Equal(1, _dicomRequestContextAccessor.RequestContext.PartCount);
        Assert.Equal(resultStream.Length, _dicomRequestContextAccessor.RequestContext.BytesRendered);

        var retrieveRenderedRequest2 = new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Frames, 2, 75, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader(mediaType: KnownContentTypes.ImagePng) });

        _fileStore.GetFileAsync(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Partition, DefaultFileProperties, DefaultCancellationToken).Returns(streamAndStoredFileForFrame2);
        RetrieveRenderedResponse response2 = await _retrieveRenderedService.RetrieveRenderedImageAsync(
               retrieveRenderedRequest2,
               DefaultCancellationToken);

        copyStream.Position = 0;
        using var img2 = dicomImage.RenderImage(1);
        using var sharpImage2 = img2.AsSharpImage();
        using MemoryStream resultStream2 = _recyclableMemoryStreamManager.GetStream();
        await sharpImage2.SaveAsPngAsync(resultStream2, new SixLabors.ImageSharp.Formats.Png.PngEncoder(), DefaultCancellationToken);
        resultStream2.Position = 0;
        AssertStreamsEqual(resultStream2, response2.ResponseStream);
        Assert.Equal("image/png", response.ContentType);
        Assert.Equal(1, _dicomRequestContextAccessor.RequestContext.PartCount);
        Assert.Equal(resultStream2.Length, _dicomRequestContextAccessor.RequestContext.BytesRendered);

        copyStream.Dispose();
        streamAndStoredFileForFrame2.Dispose();
        response.ResponseStream.Dispose();
        response2.ResponseStream.Dispose();

    }

    [Fact]
    public async Task GivenStoredInstances_WhenRetrieveRenderedWithoutSpecifyingAcceptHeaders_ThenRenderJpegSuccesfully()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(partition: _dicomRequestContextAccessor.RequestContext.DataPartition);

        KeyValuePair<DicomFile, Stream> streamAndStoredFile = await RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier), _recyclableMemoryStreamManager, frames: 3);

        MemoryStream copyStream = _recyclableMemoryStreamManager.GetStream();
        await streamAndStoredFile.Value.CopyToAsync(copyStream);
        copyStream.Position = 0;

        streamAndStoredFile.Value.Position = 0;

        _fileStore.GetFileAsync(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Partition, DefaultFileProperties, DefaultCancellationToken).Returns(streamAndStoredFile.Value);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Version, versionedInstanceIdentifiers[0].VersionedInstanceIdentifier.Partition, DefaultFileProperties, DefaultCancellationToken).Returns(new FileProperties { ContentLength = streamAndStoredFile.Value.Length });

        var retrieveRenderedRequest = new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Instance, 1, 75, new List<AcceptHeader>());

        RetrieveRenderedResponse response = await _retrieveRenderedService.RetrieveRenderedImageAsync(
               retrieveRenderedRequest,
               DefaultCancellationToken);

        DicomFile dicomFile = await DicomFile.OpenAsync(copyStream, FileReadOption.ReadLargeOnDemand);
        DicomImage dicomImage = new DicomImage(dicomFile.Dataset);
        using var img = dicomImage.RenderImage(0);
        using var sharpImage = img.AsSharpImage();
        using MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream();
        await sharpImage.SaveAsJpegAsync(resultStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder(), DefaultCancellationToken);
        resultStream.Position = 0;
        AssertStreamsEqual(resultStream, response.ResponseStream);
        Assert.Equal("image/jpeg", response.ContentType);
        Assert.Equal(1, _dicomRequestContextAccessor.RequestContext.PartCount);
        Assert.Equal(resultStream.Length, _dicomRequestContextAccessor.RequestContext.BytesRendered);

        response.ResponseStream.Dispose();
        copyStream.Dispose();
    }

    private List<InstanceMetadata> SetupInstanceIdentifiersList(Partition partition = null, InstanceProperties instanceProperty = null)
    {
        var dicomInstanceIdentifiersList = new List<InstanceMetadata>();

        instanceProperty ??= new InstanceProperties { FileProperties = DefaultFileProperties };
        partition ??= Partition.Default;

        dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0, partition), instanceProperty));
        _instanceStore.GetInstanceIdentifierWithPropertiesAsync(dicomInstanceIdentifiersList[0].VersionedInstanceIdentifier.Partition, _studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, false, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);

        return dicomInstanceIdentifiersList;
    }

    private static void AssertStreamsEqual(Stream expectedPixelData, Stream actualPixelData)
    {
        Assert.Equal(expectedPixelData.Length, actualPixelData.Length);
        Assert.Equal(0, actualPixelData.Position);
        Assert.Equal(0, expectedPixelData.Position);
        for (var i = 0; i < expectedPixelData.Length; i++)
        {
            Assert.Equal(expectedPixelData.ReadByte(), actualPixelData.ReadByte());
        }
    }
}
