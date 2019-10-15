// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Microsoft.Health.Dicom.Core.Features.Resources.Retrieve.BitmapRendering;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    public static class StreamExtensions
    {
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

            // Since requesting to render a multiframe image without specifying a frame is ambiguous, per DICOM spec
            // we are free to make assumptions here. We will render the first frame by default
            return new DicomImage(tempDicomFile.Dataset).ToRenderedMemoryStream(imageRepresentation, frame: 0, thumbnail);
        }
    }
}
