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
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Resources.Retrieve.BitmapRendering;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    public static class DicomFileExtensions
    {
        private static readonly Size FrameThumbnailSize = new Size(width: 100, height: 100);

        public static Stream GetFrameAsDicomData(this DicomFile dicomFile, int frame, DicomTransferSyntax requestedTransferSyntax)
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

        public static Stream GetFrameAsImage(this DicomFile dicomFile, int frame, ImageRepresentationModel imageRepresentation, bool thumbnail)
        {
            var ms = new MemoryStream();

            try
            {
                using (var image = new DicomImage(dicomFile.Dataset).RenderImage(frame).AsClonedBitmap())
                {
                    var bmp = image;
                    if (thumbnail)
                    {
                        var bmpResized = new Bitmap(FrameThumbnailSize.Width, FrameThumbnailSize.Height);
                        using (var graphics = Graphics.FromImage(bmpResized))
                        {
                            graphics.CompositingQuality = CompositingQuality.HighSpeed;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.DrawImage(image, x: 0, y: 0, FrameThumbnailSize.Width, FrameThumbnailSize.Height);
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

        public static void ValidateHasFrames(this DicomFile dicomFile, IEnumerable<int> frames)
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

            // Note: We look for any frame value that is less than zero, or greater than number of frames.
            var pixelData = DicomPixelData.Create(dataset);
            var missingFrames = frames.Where(x => x >= pixelData.NumberOfFrames || x < 0).ToArray();

            // If any missing frames, throw not found exception for the specific frames not found.
            if (missingFrames.Length > 0)
            {
                throw new DataStoreException(HttpStatusCode.NotFound, new ArgumentException($"The frame(s) '{string.Join(", ", missingFrames)}' do not exist.", nameof(frames)));
            }
        }
    }
}
