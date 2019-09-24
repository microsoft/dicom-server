// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Microsoft.Health.Dicom.Core.Features.Resources.Retrieve.BitmapRendering;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    public static class StreamExtensions
    {
        private static readonly Size FileThumbnailSize = new Size(width: 100, height: 100);

        public static Stream EncodeDicomFileAsDicom(this Stream stream, DicomTransferSyntax requestedTransferSyntax)
        {
            var tempDicomFile = DicomFile.Open(stream);

            // If the DICOM file is already in the requested transfer syntax, return the base stream, otherwise re-encode.
            if (tempDicomFile.Dataset.InternalTransferSyntax == requestedTransferSyntax)
            {
                stream.Seek(offset: 0, SeekOrigin.Begin);
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
                    resultStream.Seek(offset: 0, loc: SeekOrigin.Begin);
                }

                // We can dispose of the base stream as this is not needed.
                stream.Dispose();
                return resultStream;
            }
        }

        public static Stream EncodeDicomFileAsImage(this Stream stream, ImageRepresentationModel imageRepresentation, bool thumbnail)
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
                        var bmpResized = new Bitmap(FileThumbnailSize.Width, FileThumbnailSize.Height);
                        using (var graphics = Graphics.FromImage(bmpResized))
                        {
                            graphics.CompositingQuality = CompositingQuality.HighSpeed;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.DrawImage(image, x: 0, y: 0, FileThumbnailSize.Width, FileThumbnailSize.Height);
                        }

                        bmp = bmpResized;
                    }

                    bmp.Save(ms, imageRepresentation.CodecInfo, imageRepresentation.EncoderParameters);
                }

                ms.Seek(offset: 0, loc: SeekOrigin.Begin);
            }
            catch
            {
                // We catch all here because rendering may throw for a variety of reasons.
                // Most likely, if we get here, this one is a corrupt image and we should return empty
            }

            return ms;
        }
    }
}
