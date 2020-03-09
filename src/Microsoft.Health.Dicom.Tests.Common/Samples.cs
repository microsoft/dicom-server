// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;
using EnsureThat;

namespace Microsoft.Health.Dicom.Tests.Common
{
    public static class Samples
    {
        private static readonly Random _random = new Random();

        public static IEnumerable<DicomFile> GetDicomFilesForTranscoding()
        {
            var directory = @"TranscodingSamples";
            return Directory.EnumerateFiles(directory, "*.dcm", SearchOption.AllDirectories).Select(
                path => DicomFile.Open(path));
        }

        public static IEnumerable<DicomFile> GetSampleDicomFiles()
        {
            var directory = @"ImageSamples";
            return Directory.EnumerateFiles(directory, "*.dcm", SearchOption.AllDirectories).Select(
                path => DicomFile.Open(path));
        }

        /// <summary>
        /// Will generate a file with valid 8bit pixel data representing a monochrome pattern.
        /// </summary>
        /// <param name="studyInstanceUID">Study UID (will generate new if null)</param>
        /// <param name="seriesInstanceUID">Series UID (will generate new if null)</param>
        /// <param name="sopInstanceUID">Instance UID (will generate new if null)</param>
        /// <param name="rows">width</param>
        /// <param name="columns">height</param>
        /// <param name="transferSyntax">Transfer Syntax</param>
        /// <param name="encode">Whether to encode image according to the supplied transfer syntax. False might result in invalid images</param>
        /// <param name="frames">Number of frames to generate</param>
        /// <returns>DicomFile</returns>
        public static DicomFile CreateRandomDicomFileWith8BitPixelData(
            string studyInstanceUID = null,
            string seriesInstanceUID = null,
            string sopInstanceUID = null,
            int rows = 512,
            int columns = 512,
            string transferSyntax = "1.2.840.10008.1.2.1",  // Explicit VR Little Endian
            bool encode = true,
            int frames = 1)
        {
            DicomFile dicomFile = DicomImageGenerator.GenerateDicomFile(
               studyInstanceUID,
               seriesInstanceUID,
               sopInstanceUID,
               null,
               rows,
               columns,
               TestFileBitDepth.EightBit,
               transferSyntax,
               encode,
               frames);

            return dicomFile;
        }

        /// <summary>
        /// Will generate a file with valid 16bit pixel data representing a monochrome pattern.
        /// </summary>
        /// <param name="studyInstanceUID">Study UID (will generate new if null)</param>
        /// <param name="seriesInstanceUID">Series UID (will generate new if null)</param>
        /// <param name="sopInstanceUID">Instance UID (will generate new if null)</param>
        /// <param name="rows">width</param>
        /// <param name="columns">height</param>
        /// <param name="transferSyntax">Transfer Syntax</param>
        /// <param name="encode">Whether to encode image according to the supplied transfer syntax. False might result in invalid images</param>
        /// /// <param name="frames">Number of frames to generate</param>
        /// <returns>DicomFile</returns>
        public static DicomFile CreateRandomDicomFileWith16BitPixelData(
            string studyInstanceUID = null,
            string seriesInstanceUID = null,
            string sopInstanceUID = null,
            int rows = 512,
            int columns = 512,
            string transferSyntax = "1.2.840.10008.1.2.1", // Explicit VR Little Endian
            bool encode = true,
            int frames = 1)
        {
            DicomFile dicomFile = DicomImageGenerator.GenerateDicomFile(
                studyInstanceUID,
                seriesInstanceUID,
                sopInstanceUID,
                null,
                rows,
                columns,
                TestFileBitDepth.SixteenBit,
                transferSyntax,
                encode,
                frames);

            return dicomFile;
        }

        public static void AppendRandomPixelData(int rows, int columns, int frames, params DicomFile[] dicomFiles)
        {
            EnsureArg.IsGte(rows, 0, nameof(rows));
            EnsureArg.IsGte(columns, 0, nameof(columns));
            EnsureArg.IsNotNull(dicomFiles, nameof(dicomFiles));

            var pixelDataSize = rows * columns;
            const ushort bitsAllocated = 8;
            dicomFiles.Each(x =>
            {
                x.Dataset.AddOrUpdate(DicomTag.Rows, (ushort)rows);
                x.Dataset.AddOrUpdate(DicomTag.Columns, (ushort)columns);
                x.Dataset.AddOrUpdate(DicomTag.BitsAllocated, bitsAllocated);

                var pixelData = DicomPixelData.Create(x.Dataset, true);
                pixelData.SamplesPerPixel = 1;
                pixelData.PixelRepresentation = PixelRepresentation.Unsigned;
                pixelData.BitsStored = bitsAllocated;
                pixelData.HighBit = bitsAllocated - 1;

                for (var i = 0; i < frames; i++)
                {
                    pixelData.AddFrame(CreateRandomPixelData(pixelDataSize));
                }
            });
        }

        public static DicomFile CreateRandomDicomFileWithPixelData(
        string studyInstanceUID = null,
        string seriesInstanceUID = null,
        string sopInstanceUID = null,
        int rows = 50,
        int columns = 50,
        int frames = 1)
        {
            var result = new DicomFile(CreateRandomInstanceDataset(studyInstanceUID, seriesInstanceUID, sopInstanceUID));
            AppendRandomPixelData(rows, columns, frames, result);
            return result;
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
                { DicomTag.StudyInstanceUID, studyInstanceUID ?? TestUidGenerator.Generate() },
                { DicomTag.SeriesInstanceUID, seriesInstanceUID ?? TestUidGenerator.Generate() },
                { DicomTag.SOPInstanceUID, sopInstanceUID ?? TestUidGenerator.Generate() },
                { DicomTag.SOPClassUID, sopClassUID ?? TestUidGenerator.Generate() },
                { DicomTag.BitsAllocated, (ushort)8 },
                { DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value },
            };

            return ds;
        }

        private static IByteBuffer CreateRandomPixelData(int pixelDataSize)
        {
            var result = new byte[pixelDataSize];
            for (var i = 0; i < pixelDataSize; i++)
            {
                result[i] = (byte)_random.Next(0, 255);
            }

            return new MemoryByteBuffer(result);
        }
    }
}
