// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;
public class RetrieveRenderedService : IRetrieveRenderedService
{
    private readonly IFileStore _blobDataStore;
    private readonly IInstanceStore _instanceStore;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly RetrieveConfiguration _retrieveConfiguration;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly ILogger<RetrieveRenderedService> _logger;
    private readonly RetrieveMeter _retrieveMeter;

    public RetrieveRenderedService(
        IInstanceStore instanceStore,
        IFileStore blobDataStore,
        IDicomRequestContextAccessor dicomRequestContextAccessor,
        IOptionsSnapshot<RetrieveConfiguration> retrieveConfiguration,
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        RetrieveMeter retrieveMeter,
        ILogger<RetrieveRenderedService> logger)
    {
        EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
        EnsureArg.IsNotNull(blobDataStore, nameof(blobDataStore));
        EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));
        EnsureArg.IsNotNull(retrieveConfiguration?.Value, nameof(retrieveConfiguration));
        EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
        _retrieveMeter = EnsureArg.IsNotNull(retrieveMeter, nameof(retrieveMeter));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _instanceStore = instanceStore;
        _blobDataStore = blobDataStore;
        _dicomRequestContextAccessor = dicomRequestContextAccessor;
        _retrieveConfiguration = retrieveConfiguration?.Value;
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        _logger = logger;
    }

    public async Task<RetrieveRenderedResponse> RetrieveRenderedImageAsync(RetrieveRenderedRequest request, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(request, nameof(request));

        if (request.Quality < 1 || request.Quality > 100)
        {
            throw new BadRequestException(DicomCoreResource.InvalidImageQuality);
        }

        // To keep track of how long render operation is taking
        Stopwatch sw = new Stopwatch();

        var partition = _dicomRequestContextAccessor.RequestContext.GetPartition();
        _dicomRequestContextAccessor.RequestContext.PartCount = 1;
        AcceptHeader returnHeader = GetValidRenderAcceptHeader(request.AcceptHeaders);

        try
        {
            // this call throws NotFound when zero instance found
            InstanceMetadata instance = (await _instanceStore.GetInstancesWithProperties(
                ResourceType.Instance, partition, request.StudyInstanceUid, request.SeriesInstanceUid, request.SopInstanceUid, isInitialVersion: false, cancellationToken))[0];

            FileProperties fileProperties = await RetrieveHelpers.CheckFileSize(_blobDataStore, _retrieveConfiguration.MaxDicomFileSize, instance.VersionedInstanceIdentifier.Version, partition, instance.InstanceProperties.FileProperties, true, _logger, cancellationToken);
            _logger.LogInformation(
                "Retrieving rendered Instance for watermark {Watermark} of size {ContentLength}", instance.VersionedInstanceIdentifier.Version, fileProperties.ContentLength);
            _retrieveMeter.RetrieveInstanceCount.Add(
                fileProperties.ContentLength,
                RetrieveMeter.RetrieveInstanceCountTelemetryDimension(isRendered: true));

            using Stream stream = await _blobDataStore.GetFileAsync(instance.VersionedInstanceIdentifier.Version, instance.VersionedInstanceIdentifier.Partition, instance.InstanceProperties.FileProperties, cancellationToken);
            sw.Start();

            DicomFile dicomFile = await DicomFile.OpenAsync(stream, FileReadOption.ReadLargeOnDemand);
            DicomPixelData dicomPixelData = dicomFile.GetPixelDataAndValidateFrames(new[] { request.FrameNumber });

            Stream resultStream = await ConvertToImage(dicomFile, request.FrameNumber, returnHeader.MediaType.ToString(), request.Quality, cancellationToken);
            string outputContentType = returnHeader.MediaType.ToString();

            sw.Stop();
            _logger.LogInformation("Render from dicom to {OutputContentType}, uncompressed file size was {UncompressedFrameSize}, output frame size is {OutputFrameSize} and took {ElapsedMilliseconds} ms", outputContentType, stream.Length, resultStream.Length, sw.ElapsedMilliseconds);

            _dicomRequestContextAccessor.RequestContext.BytesRendered = resultStream.Length;

            return new RetrieveRenderedResponse(resultStream, resultStream.Length, outputContentType);
        }

        catch (DataStoreException e)
        {
            // Log request details associated with exception. Note that the details are not for the store call that failed but for the request only.
            _logger.LogError(e, "Error retrieving dicom resource to render");
            throw;
        }

    }

    private async Task<Stream> ConvertToImage(DicomFile dicomFile, int frameNumber, string mediaType, int quality, CancellationToken cancellationToken)
    {
        try
        {
            DicomImage dicomImage = new DicomImage(dicomFile.Dataset);
            using var img = dicomImage.RenderImage(frameNumber);
            using var sharpImage = img.AsSharpImage();
            MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream(tag: nameof(ConvertToImage));

            if (mediaType.Equals(KnownContentTypes.ImageJpeg, StringComparison.OrdinalIgnoreCase))
            {
                JpegEncoder jpegEncoder = new JpegEncoder();
                jpegEncoder.Quality = quality;
                await sharpImage.SaveAsJpegAsync(resultStream, jpegEncoder, cancellationToken: cancellationToken);
            }
            else
            {
                await sharpImage.SaveAsPngAsync(resultStream, new SixLabors.ImageSharp.Formats.Png.PngEncoder(), cancellationToken: cancellationToken);
            }

            resultStream.Position = 0;

            return resultStream;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error rendering dicom file into {OutputConentType} media type", mediaType);
            throw new DicomImageException();
        }
    }

    private static AcceptHeader GetValidRenderAcceptHeader(IReadOnlyCollection<AcceptHeader> acceptHeaders)
    {
        EnsureArg.IsNotNull(acceptHeaders, nameof(acceptHeaders));

        if (acceptHeaders.Count > 1)
        {
            throw new NotAcceptableException(DicomCoreResource.MultipleAcceptHeadersNotSupported);
        }

        if (acceptHeaders.Count == 1)
        {
            var mediaType = acceptHeaders.First()?.MediaType;

            if (mediaType == null || (!StringSegment.Equals(mediaType.ToString(), KnownContentTypes.ImageJpeg, StringComparison.InvariantCultureIgnoreCase) && !StringSegment.Equals(mediaType.ToString(), KnownContentTypes.ImagePng, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new NotAcceptableException(DicomCoreResource.NotAcceptableHeaders);
            }

            if (StringSegment.Equals(mediaType.ToString(), KnownContentTypes.ImagePng, StringComparison.InvariantCultureIgnoreCase))
            {
                return new AcceptHeader(KnownContentTypes.ImagePng, PayloadTypes.SinglePart);
            }
        }

        return new AcceptHeader(KnownContentTypes.ImageJpeg, PayloadTypes.SinglePart);
    }
}
