// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Resources.Retrieve.BitmapRendering;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    public class RetrieveDicomResourceHandler : BaseRetrieveDicomResourceHandler, IRequestHandler<RetrieveDicomResourceRequest, RetrieveDicomResourceResponse>
    {
        private static readonly DicomTransferSyntax DefaultTransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
        private readonly DicomDataStore _dicomDataStore;

        private static DicomTransferSyntax[] _supportedTransferSyntaxes8bit = new[]
        {
            DicomTransferSyntax.DeflatedExplicitVRLittleEndian,
            DicomTransferSyntax.ExplicitVRBigEndian,
            DicomTransferSyntax.ExplicitVRLittleEndian,
            DicomTransferSyntax.ImplicitVRLittleEndian,
            DicomTransferSyntax.JPEG2000Lossless,
            DicomTransferSyntax.JPEG2000Lossy,
            DicomTransferSyntax.JPEGProcess1,
            DicomTransferSyntax.JPEGProcess2_4,
            DicomTransferSyntax.RLELossless,
        };

        private static DicomTransferSyntax[] _supportedTransferSyntaxesOver8bit = new[]
        {
            DicomTransferSyntax.DeflatedExplicitVRLittleEndian,
            DicomTransferSyntax.ExplicitVRBigEndian,
            DicomTransferSyntax.ExplicitVRLittleEndian,
            DicomTransferSyntax.ImplicitVRLittleEndian,
            DicomTransferSyntax.RLELossless,
        };

        private static readonly Size ThumbnailSize = new Size(100, 100);

        public RetrieveDicomResourceHandler(IDicomMetadataStore dicomMetadataStore, DicomDataStore dicomDataStore)
            : base(dicomMetadataStore)
        {
            EnsureArg.IsNotNull(dicomDataStore, nameof(dicomDataStore));

            _dicomDataStore = dicomDataStore;
        }

        private bool CanTranscodeDataset(DicomDataset ds, DicomTransferSyntax toTransferSyntax)
        {
            if (toTransferSyntax == null)
            {
               return true;
            }

            var fromTs = ds.InternalTransferSyntax;
            if (!ds.TryGetSingleValue(DicomTag.BitsAllocated, out ushort bpp))
            {
                return false;
            }

            if (!ds.TryGetString(DicomTag.PhotometricInterpretation, out string photometricInterpretation))
            {
                return false;
            }

            // Bug in fo-dicom 4.0.1
            if ((toTransferSyntax == DicomTransferSyntax.JPEGProcess1 || toTransferSyntax == DicomTransferSyntax.JPEGProcess2_4) &&
                ((photometricInterpretation == PhotometricInterpretation.Monochrome2.Value) ||
                 (photometricInterpretation == PhotometricInterpretation.Monochrome1.Value)))
            {
                return false;
            }

            if (((bpp > 8) && _supportedTransferSyntaxesOver8bit.Contains(toTransferSyntax) && _supportedTransferSyntaxesOver8bit.Contains(fromTs)) ||
                 ((bpp <= 8) && _supportedTransferSyntaxes8bit.Contains(toTransferSyntax) && _supportedTransferSyntaxes8bit.Contains(fromTs)))
            {
                return true;
            }

            return false;
        }

        public async Task<RetrieveDicomResourceResponse> Handle(
            RetrieveDicomResourceRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            try
            {
                IEnumerable<DicomInstance> retrieveInstances = await GetInstancesToRetrieve(
                    message.ResourceType, message.StudyInstanceUID, message.SeriesInstanceUID, message.SopInstanceUID, cancellationToken);
                Stream[] resultStreams = await Task.WhenAll(retrieveInstances.Select(x => _dicomDataStore.GetDicomDataStreamAsync(x, cancellationToken)));

                var responseCode = HttpStatusCode.OK;

                DicomTransferSyntax parsedDicomTransferSyntax =
                    message.RenderedRequested ? null :
                    message.OriginalTransferSyntaxRequested() ? null :
                    string.IsNullOrWhiteSpace(message.RequestedRepresentation) ? DefaultTransferSyntax :
                    DicomTransferSyntax.Parse(message.RequestedRepresentation);

                ImageRepresentationModel imageRepresentation =
                    message.RenderedRequested ?
                        ImageRepresentationModel.Parse(message.RequestedRepresentation) :
                        null;

                if (message.ResourceType == ResourceType.Frames)
                {
                    // We first validate the file has the requested frames, then pass the frame for lazy encoding.
                    var dicomFile = DicomFile.Open(resultStreams.Single());
                    ValidateHasFrames(dicomFile, message.Frames);

                    if (message.RenderedRequested)
                    {
                        resultStreams = message.Frames.Select(
                                x => new LazyTransformReadOnlyStream<DicomFile>(
                                    dicomFile,
                                    y => GetFrameAsImage(y, x - 1, imageRepresentation, message.ThumbnailRequested)))
                            .ToArray();
                    }
                    else
                    {
                        if (!message.OriginalTransferSyntaxRequested() &&
                            !CanTranscodeDataset(dicomFile.Dataset, parsedDicomTransferSyntax))
                        {
                            throw new DataStoreException(HttpStatusCode.NotAcceptable);
                        }

                        // Note that per DICOMWeb spec (http://dicom.nema.org/medical/dicom/current/output/html/part18.html#sect_9.5.1.2.1)
                        // frame number in the URI is 1-based, unlike fo-dicom representation
                        resultStreams = message.Frames.Select(
                                x => new LazyTransformReadOnlyStream<DicomFile>(
                                    dicomFile,
                                    y => GetFrameAsDicomData(y, x - 1, parsedDicomTransferSyntax)))
                            .ToArray();
                    }
                }
                else
                {
                    if (message.RenderedRequested)
                    {
                        resultStreams = resultStreams.Select(x =>
                            new LazyTransformReadOnlyStream<Stream>(
                                x,
                                y => EncodeDicomFileAsImage(y, imageRepresentation, message.ThumbnailRequested))).ToArray();
                    }
                    else
                    {
                        if (!message.OriginalTransferSyntaxRequested())
                        {
                            resultStreams = resultStreams.Where(x =>
                            {
                                var canTranscode = false;

                                try
                                {
                                    // TODO: replace with FileReadOption.SkipLargeTags when updating to a future
                                    // version of fo-dicom where https://github.com/fo-dicom/fo-dicom/issues/893 is fixed
                                    var dicomFile = DicomFile.Open(x, FileReadOption.ReadLargeOnDemand);
                                    canTranscode = CanTranscodeDataset(dicomFile.Dataset, parsedDicomTransferSyntax);
                                }
                                catch (DicomFileException)
                                {
                                    canTranscode = false;
                                }

                                x.Seek(0, SeekOrigin.Begin);

                                // If some of the instances are not transcodeable, Partial Content should be returned
                                if (!canTranscode)
                                {
                                    responseCode = HttpStatusCode.PartialContent;
                                }

                                return canTranscode;
                            }).ToArray();
                        }

                        if (resultStreams.Length == 0)
                        {
                            throw new DataStoreException(HttpStatusCode.NotAcceptable);
                        }

                        resultStreams = resultStreams.Select(x =>
                            new LazyTransformReadOnlyStream<Stream>(
                                x,
                                y => EncodeDicomFileAsDicom(y, parsedDicomTransferSyntax))).ToArray();
                    }
                }

                return new RetrieveDicomResourceResponse(responseCode, resultStreams);
            }
            catch (DataStoreException e)
            {
                return new RetrieveDicomResourceResponse(e.StatusCode);
            }
        }

        private static Stream EncodeDicomFileAsDicom(Stream stream, DicomTransferSyntax requestedTransferSyntax)
        {
            var tempDicomFile = DicomFile.Open(stream);

            // If the DICOM file is already in the requested transfer syntax, return the base stream, otherwise re-encode.
            if (tempDicomFile.Dataset.InternalTransferSyntax == requestedTransferSyntax)
            {
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
            else
            {
                if (requestedTransferSyntax != null)
                {
                    try
                    {
                        var transcoder = new DicomTranscoder(
                            tempDicomFile.Dataset.InternalTransferSyntax,
                            requestedTransferSyntax);
                        tempDicomFile = transcoder.Transcode(tempDicomFile);
                    }

                    // We catch all here as Transcoder can throw a wide variety of things.
                    // Basically this means codec failure - a quite extraordinary situation, but not impossible
                    catch
                    {
                        tempDicomFile = null;
                    }
                }

                var resultStream = new MemoryStream();

                if (tempDicomFile != null)
                {
                    tempDicomFile.Save(resultStream);
                    resultStream.Seek(0, SeekOrigin.Begin);
                }

                // We can dispose of the base stream as this is not needed.
                stream.Dispose();
                return resultStream;
            }
        }

        private static Stream EncodeDicomFileAsImage(Stream stream, ImageRepresentationModel imageRepresentation, bool thumbnail)
        {
            var tempDicomFile = DicomFile.Open(stream);
            var ms = new MemoryStream();

            try
            {
                using (var image = new DicomImage(tempDicomFile.Dataset).RenderImage().AsClonedBitmap())
                {
                    var bmp = image;
                    if (thumbnail)
                    {
                        var bmpResized = new Bitmap(ThumbnailSize.Width, ThumbnailSize.Height);
                        using (var graphics = Graphics.FromImage(bmpResized))
                        {
                            graphics.CompositingQuality = CompositingQuality.HighSpeed;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.DrawImage(image, 0, 0, ThumbnailSize.Width, ThumbnailSize.Height);
                        }

                        bmp = bmpResized;
                    }

                    bmp.Save(ms, imageRepresentation.CodecInfo, imageRepresentation.EncoderParameters);
                }

                ms.Seek(0, SeekOrigin.Begin);
            }
            catch
            {
                // We catch all here because rendering may throw for a variety of reasons.
                // Most likely, this is a corrupt image
            }

            return ms;
        }

        private static Stream GetFrameAsDicomData(DicomFile dicomFile, int frame, DicomTransferSyntax requestedTransferSyntax)
        {
            DicomDataset dataset = dicomFile.Dataset;
            IByteBuffer resultByteBuffer;

            if (dataset.InternalTransferSyntax.IsEncapsulated && (requestedTransferSyntax != null))
            {
                // Decompress single frame from source dataset
                var transcoder = new DicomTranscoder(dataset.InternalTransferSyntax, requestedTransferSyntax);
                resultByteBuffer = transcoder.DecodeFrame(dataset, frame);
            }
            else
            {
                // Pull uncompressed frame from source pixel data
                var pixelData = DicomPixelData.Create(dataset);
                if (frame >= pixelData.NumberOfFrames)
                {
                    throw new DataStoreException(HttpStatusCode.NotFound, new ArgumentException($"The frame '{frame}' does not exist.", nameof(frame)));
                }

                resultByteBuffer = pixelData.GetFrame(frame);
            }

            return new MemoryStream(resultByteBuffer.Data);
        }

        private static Stream GetFrameAsImage(DicomFile dicomFile, int frame, ImageRepresentationModel imageRepresentation, bool thumbnail)
        {
            var ms = new MemoryStream();

            try
            {
                using (var image = new DicomImage(dicomFile.Dataset).RenderImage(frame).AsClonedBitmap())
                {
                    var bmp = image;
                    if (thumbnail)
                    {
                        var bmpResized = new Bitmap(ThumbnailSize.Width, ThumbnailSize.Height);
                        using (var graphics = Graphics.FromImage(bmpResized))
                        {
                            graphics.CompositingQuality = CompositingQuality.HighSpeed;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.DrawImage(image, 0, 0, ThumbnailSize.Width, ThumbnailSize.Height);
                        }

                        bmp = bmpResized;
                    }

                    bmp.Save(ms, imageRepresentation.CodecInfo, imageRepresentation.EncoderParameters);
                }

                ms.Seek(0, SeekOrigin.Begin);
            }
            catch
            {
                // We catch all here because rendering may throw for a variety of reasons.
                // Most likely, this is a corrupt image
            }

            return ms;
        }

        private static void ValidateHasFrames(DicomFile dicomFile, IEnumerable<int> frames)
        {
            DicomDataset dataset = dicomFile.Dataset;

            // Validate the dataset has the correct DICOM tags.
            if (!dataset.Contains(DicomTag.BitsAllocated) ||
                !dataset.Contains(DicomTag.Columns) ||
                !dataset.Contains(DicomTag.Rows) ||
                !dataset.Contains(DicomTag.PixelData))
            {
                throw new DataStoreException(HttpStatusCode.NotFound);
            }

            // Note: We look for any frame value that is 0 or less, or greater than number of frames.
            // As number of frames is 0 based, but incoming frame requests start at 1, we are converting in this check.
            var pixelData = DicomPixelData.Create(dataset);
            var missingFrames = frames.Where(x => x > pixelData.NumberOfFrames || x <= 0).ToArray();

            // If any missing frames, throw not found exception for the specific frames not found.
            if (missingFrames.Length > 0)
            {
                throw new DataStoreException(HttpStatusCode.NotFound, new ArgumentException($"The frame(s) '{string.Join(", ", missingFrames)}' do not exist.", nameof(frames)));
            }
        }
    }
}
