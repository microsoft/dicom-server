// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;

namespace Microsoft.Health.Dicom.Tests.Common
{
    public class Generator
    {
        private static byte[] GetBytes(int rows, int cols)
        {
            var pixelDataSize = rows * cols;
            var bytes = new byte[pixelDataSize];

            for (var i = 0; i < pixelDataSize; i++)
            {
                var x = i % rows;
                var y = i / cols;

                bytes[i] = (byte)Math.Clamp(
                    16 * (Math.Round(16.0f * x / (rows + cols)) + Math.Round(16.0f * y * y / cols / (rows + cols))),
                    0,
                    255);
            }

            return bytes;
        }

        public static DicomFile GenerateDicomFile(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, string sopClassUID, string transferSyntax)
        {
            var rows = 512;
            var cols = 512;

            var dicomFile = new DicomFile(
                new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
                {
                    { DicomTag.StudyInstanceUID, studyInstanceUID ?? Guid.NewGuid().ToString() },
                    { DicomTag.SeriesInstanceUID, seriesInstanceUID ?? Guid.NewGuid().ToString() },
                    { DicomTag.SOPInstanceUID, sopInstanceUID ?? Guid.NewGuid().ToString() },
                    { DicomTag.SOPClassUID, sopClassUID ?? Guid.NewGuid().ToString() },
                    { DicomTag.Rows, (ushort)rows },
                    { DicomTag.Columns, (ushort)cols },
                    { DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value },
                    { DicomTag.BitsAllocated, (ushort)8 },
                });

            var pixelData = DicomPixelData.Create(dicomFile.Dataset, true);
            pixelData.SamplesPerPixel = 1;
            pixelData.BitsStored = 8;
            pixelData.HighBit = 7;
            pixelData.PixelRepresentation = PixelRepresentation.Unsigned;

            var buffer = new MemoryByteBuffer(GetBytes(rows, cols));
            pixelData.AddFrame(buffer);

            if (transferSyntax != DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID)
            {
                var transcoder =
                    new DicomTranscoder(dicomFile.Dataset.InternalTransferSyntax, DicomTransferSyntax.Parse(transferSyntax));
                dicomFile = transcoder.Transcode(dicomFile);
            }

            return dicomFile;
        }
    }
}
