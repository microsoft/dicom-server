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
    public enum TestFileBitDepth : ushort
    {
        EightBit = 8,
        SixteenBit = 16,
    }

    public class DicomImageGenerator
    {
        private static byte[] GetBytesFor8BitImage(int rows, int cols)
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

        private static byte[] GetBytesFor16BitImage(int rows, int cols)
        {
            var pixelDataSize = rows * cols;
            var result = new byte[pixelDataSize * 2];

            for (var i = 0; i < pixelDataSize * 2; i += 2)
            {
                var x = (i / 2) % rows;
                var y = (i / 2) / cols;

                // We want something with sharp gradients, full range and non-symmetric
                ushort pixel = (ushort)Math.Clamp(
                    4069.0f * (Math.Round(16.0f * x / (rows + cols)) + Math.Round(16.0f * y * y / cols / (rows + cols))),
                    0,
                    ushort.MaxValue);

                result[i + 1] = (byte)(pixel >> 8);
                result[i] = (byte)(pixel & 0xff);
            }

            return result;
        }

        public static DicomFile GenerateDicomFile(
            string studyInstanceUID,
            string seriesInstanceUID,
            string sopInstanceUID,
            string sopClassUID,
            int rows,
            int cols,
            TestFileBitDepth bitDepth,
            string transferSyntax)
        {
            var dicomFile = new DicomFile(
                new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
                {
                    { DicomTag.StudyInstanceUID, studyInstanceUID ?? DicomUID.Generate().UID },
                    { DicomTag.SeriesInstanceUID, seriesInstanceUID ?? DicomUID.Generate().UID },
                    { DicomTag.SOPInstanceUID, sopInstanceUID ?? DicomUID.Generate().UID },
                    { DicomTag.SOPClassUID, sopClassUID ?? DicomUID.Generate().UID },
                    { DicomTag.Rows, (ushort)rows },
                    { DicomTag.Columns, (ushort)cols },
                    { DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value },
                    { DicomTag.BitsAllocated, (ushort)bitDepth },
                });

            var pixelData = DicomPixelData.Create(dicomFile.Dataset, true);
            pixelData.SamplesPerPixel = 1;
            pixelData.BitsStored = (ushort)bitDepth;
            pixelData.HighBit = (ushort)(bitDepth - 1);
            pixelData.PixelRepresentation = PixelRepresentation.Unsigned;

            var buffer = new MemoryByteBuffer(
                (bitDepth == TestFileBitDepth.SixteenBit) ?
                    GetBytesFor16BitImage(rows, cols) :
                    GetBytesFor8BitImage(rows, cols));

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
