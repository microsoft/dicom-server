// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;
using EnsureThat;

namespace Microsoft.Health.Dicom.Tests.Common
{
    public static class Samples
    {
        public static IEnumerable<DicomFile> GetDicomFilesForTranscoding()
        {
            var directory = @"TranscodingSamples";
            return Directory.EnumerateFiles(directory, "*.dcm", SearchOption.AllDirectories).Select(
                path => DicomFile.Open(path));
        }

        private static byte[] GenerateByteArray(int rows, int cols)
        {
            // var random = new Random();
            var pixelDataSize = rows * cols;
            var result = new byte[pixelDataSize];

            for (var i = 0; i < pixelDataSize; i++)
            {
                var x = i % rows;
                var y = i / cols;

                // We want something with sharp gradients, full range and non-symmetric
                result[i] = (byte)Math.Clamp(
                    16 * (Math.Round(16.0f * x / (rows + cols)) + Math.Round(16.0f * y * y / cols / (rows + cols))),
                    0,
                    255);
            }

            return result;
        }

        private static byte[] GenerateByteArrayFor16bit(int rows, int cols)
        {
            // var random = new Random();
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

                // ushort pixel = (ushort)(65536.0f * ((x + y) / (float)(rows + cols)));

                result[i + 1] = (byte)(pixel >> 8);
                result[i] = (byte)(pixel & 0xff);
            }

            return result;
        }

        public static void AppendRandomPixelData(int rows, int columns, params DicomFile[] dicomFiles)
        {
            EnsureArg.IsGte(rows, 0, nameof(rows));
            EnsureArg.IsGte(columns, 0, nameof(columns));
            EnsureArg.IsNotNull(dicomFiles, nameof(dicomFiles));

            var result = GenerateByteArray(rows, columns);

            dicomFiles.Each(x =>
            {
                x.Dataset.Add(DicomTag.PixelData, result);
                x.Dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value);
                x.Dataset.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)1);
                x.Dataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)PixelRepresentation.Unsigned);
                x.Dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)8);
                x.Dataset.AddOrUpdate(DicomTag.BitsStored, (ushort)8);
                x.Dataset.AddOrUpdate(DicomTag.HighBit, (ushort)7);
                x.Dataset.AddOrUpdate(DicomTag.Rows, (ushort)rows);
                x.Dataset.AddOrUpdate(DicomTag.Columns, (ushort)columns);
            });
        }

        public static void AppendRandom16bitPixelData(int rows, int columns, params DicomFile[] dicomFiles)
        {
            EnsureArg.IsGte(rows, 0, nameof(rows));
            EnsureArg.IsGte(columns, 0, nameof(columns));
            EnsureArg.IsNotNull(dicomFiles, nameof(dicomFiles));

            var result = GenerateByteArrayFor16bit(rows, columns);

            // MemoryByteBuffer buffer = new MemoryByteBuffer(result);

            dicomFiles.Each(x =>
            {
                x.Dataset.Add(DicomTag.PixelData, result);
                x.Dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value);
                x.Dataset.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)1);
                x.Dataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)0);
                x.Dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)16);
                x.Dataset.AddOrUpdate(DicomTag.BitsStored, (ushort)16);
                x.Dataset.AddOrUpdate(DicomTag.HighBit, (ushort)15);
                x.Dataset.AddOrUpdate(DicomTag.Rows, (ushort)rows);
                x.Dataset.AddOrUpdate(DicomTag.Columns, (ushort)columns);
            });
        }

        public static DicomFile CreateRandomDicomFileWithPixelData(
            string studyInstanceUID = null,
            string seriesInstanceUID = null,
            string sopInstanceUID = null,
            int rows = 512,
            int columns = 512,
            string transferSyntax = "1.2.840.10008.1.2.1") // Explicit VR Little Endian
        {
            var dicomFile = new DicomFile(CreateRandomInstanceDataset(studyInstanceUID, seriesInstanceUID, sopInstanceUID));
            AppendRandomPixelData(rows, columns, dicomFile);

            if (transferSyntax != DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID)
            {
                var transcoder =
                    new DicomTranscoder(dicomFile.Dataset.InternalTransferSyntax, DicomTransferSyntax.Parse(transferSyntax));
                dicomFile = transcoder.Transcode(dicomFile);
            }

            return dicomFile;
        }

        public static DicomFile CreateRandomDicomFileWith16bitPixelData(
            string studyInstanceUID = null,
            string seriesInstanceUID = null,
            string sopInstanceUID = null,
            int rows = 512,
            int columns = 512,
            string transferSyntax = "1.2.840.10008.1.2.1") // Explicit VR Little Endian
        {
            var dicomFile = new DicomFile(CreateRandomInstanceDataset(studyInstanceUID, seriesInstanceUID, sopInstanceUID));
            AppendRandom16bitPixelData(rows, columns, dicomFile);

            if (transferSyntax != DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID)
            {
                var transcoder =
                    new DicomTranscoder(dicomFile.Dataset.InternalTransferSyntax, DicomTransferSyntax.Parse(transferSyntax));
                dicomFile = transcoder.Transcode(dicomFile);
            }

            return dicomFile;
        }

        public static DicomFile CreateRandomDicomFile(
            string studyInstanceUID = null,
            string seriesInstanceUID = null,
            string sopInstanceUID = null)
        {
            return new DicomFile(CreateRandomInstanceDataset(studyInstanceUID, seriesInstanceUID, sopInstanceUID));
        }

        public static DicomDataset CreateRandomInstanceDataset(
            string studyInstanceUID = null,
            string seriesInstanceUID = null,
            string sopInstanceUID = null,
            string sopClassUID = null)
        {
            var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
            {
                { DicomTag.StudyInstanceUID, studyInstanceUID ?? Guid.NewGuid().ToString() },
                { DicomTag.SeriesInstanceUID, seriesInstanceUID ?? Guid.NewGuid().ToString() },
                { DicomTag.SOPInstanceUID, sopInstanceUID ?? Guid.NewGuid().ToString() },
                { DicomTag.SOPClassUID, sopClassUID ?? Guid.NewGuid().ToString() },
            };

            return ds;
        }
    }
}
