// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Codec;
using FellowOakDicom.IO.Buffer;

namespace Common
{
    [SuppressMessage("Design", "CA1027:Mark enums with FlagsAttribute", Justification = "Enum members are not flags")]
    public enum TestFileBitDepth : int
    {
        None = 0,
        EightBit = 8,
        SixteenBit = 16,
    }

    public static class DicomImageGenerator
    {
        private static byte[] GetBytesFor8BitImage(int rows, int cols, int seed = 0)
        {
            var pixelDataSize = rows * cols;
            var bytes = new byte[pixelDataSize];

            for (var i = 0; i < pixelDataSize; i++)
            {
                var x = i % rows;
                var y = i / cols;

                bytes[i] = (byte)(((16 * (Math.Round(16.0f * x / (rows + cols)) + Math.Round(16.0f * y * y / cols / (rows + cols)))) + (seed * 16)) % 255);
            }

            return bytes;
        }

        private static byte[] GetBytesFor16BitImage(int rows, int cols, int seed = 0)
        {
            var pixelDataSize = rows * cols;
            var result = new byte[pixelDataSize * 2];

            for (var i = 0; i < pixelDataSize * 2; i += 2)
            {
                var x = (i / 2) % rows;
                var y = (i / 2) / cols;

                // We want something with sharp gradients, full range and non-symmetric
                ushort pixel = (ushort)(
                    ((4096.0f * (Math.Round(16.0f * x / (rows + cols)) + Math.Round(16.0f * y * y / cols / (rows + cols)))) +
                    (seed * 4096)) %
                    ushort.MaxValue);

                result[i + 1] = (byte)(pixel >> 8);
                result[i] = (byte)(pixel & 0xff);
            }

            return result;
        }

        public static DicomFile GenerateDicomFile(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string sopClassUid,
            int rows,
            int cols,
            TestFileBitDepth bitDepth,
            string transferSyntax,
            bool encode,
            int frames = 1,
            string photometricInterpretation = null)
        {
            DicomTransferSyntax initialTs = DicomTransferSyntax.ExplicitVRLittleEndian;

            if (!encode)
            {
                initialTs = DicomTransferSyntax.Parse(transferSyntax);
            }

            var rand = new Random();

            var dicomFile = new DicomFile(
                new DicomDataset(initialTs)
                {
                    { DicomTag.StudyInstanceUID, studyInstanceUid ?? TestUidGenerator.Generate() },
                    { DicomTag.SeriesInstanceUID, seriesInstanceUid ?? TestUidGenerator.Generate() },
                    { DicomTag.SOPInstanceUID, sopInstanceUid ?? TestUidGenerator.Generate() },
                    { DicomTag.SOPClassUID, sopClassUid ?? TestUidGenerator.Generate() },
                    { DicomTag.Rows, (ushort)rows },
                    { DicomTag.Columns, (ushort)cols },
                    { DicomTag.PhotometricInterpretation, photometricInterpretation ?? PhotometricInterpretation.Monochrome2.Value },
                    { DicomTag.BitsAllocated, (ushort)bitDepth },
                    { DicomTag.WindowWidth, ((bitDepth == TestFileBitDepth.EightBit) ? "256" : "65536") },
                    { DicomTag.WindowCenter, ((bitDepth == TestFileBitDepth.EightBit) ? "128" : "32768") },
                    { DicomTag.AccessionNumber, rand.Next(11111111, 19999999) },
                    { DicomTag.PatientID, TestUidGenerator.Generate() },
                });

            var pixelData = DicomPixelData.Create(dicomFile.Dataset, true);
            pixelData.SamplesPerPixel = 1;
            pixelData.BitsStored = (ushort)bitDepth;
            pixelData.HighBit = (ushort)(bitDepth - 1);
            pixelData.PixelRepresentation = PixelRepresentation.Unsigned;

            for (int i = 0; i < frames; i++)
            {
                var buffer = new MemoryByteBuffer(
                    (bitDepth == TestFileBitDepth.SixteenBit)
                        ? GetBytesFor16BitImage(rows, cols, i)
                        : GetBytesFor8BitImage(rows, cols, i));

                pixelData.AddFrame(buffer);
            }

            if (encode && transferSyntax != DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID)
            {
                var transcoder =
                    new DicomTranscoder(
                        dicomFile.Dataset.InternalTransferSyntax,
                        DicomTransferSyntax.Parse(transferSyntax));
                dicomFile = transcoder.Transcode(dicomFile);
            }

            return dicomFile;
        }
    }
}
