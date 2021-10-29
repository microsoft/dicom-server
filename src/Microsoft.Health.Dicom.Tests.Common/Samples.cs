// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using FellowOakDicom.Imaging;

namespace Microsoft.Health.Dicom.Tests.Common
{
    public static class Samples
    {
        private static readonly Random Rng = new Random();

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
        /// <param name="studyInstanceUid">Study UID (will generate new if null)</param>
        /// <param name="seriesInstanceUid">Series UID (will generate new if null)</param>
        /// <param name="sopInstanceUid">Instance UID (will generate new if null)</param>
        /// <param name="rows">width</param>
        /// <param name="columns">height</param>
        /// <param name="transferSyntax">Transfer Syntax</param>
        /// <param name="encode">Whether to encode image according to the supplied transfer syntax. False might result in invalid images</param>
        /// <param name="frames">Number of frames to generate</param>
        /// <param name="photometricInterpretation">Photometric Interpretation to be set on generated file</param>
        /// <returns>DicomFile</returns>
        public static DicomFile CreateRandomDicomFileWith8BitPixelData(
            string studyInstanceUid = null,
            string seriesInstanceUid = null,
            string sopInstanceUid = null,
            int rows = 512,
            int columns = 512,
            string transferSyntax = "1.2.840.10008.1.2.1",  // Explicit VR Little Endian
            bool encode = true,
            int frames = 1,
            string photometricInterpretation = null)
        {
            DicomFile dicomFile = DicomImageGenerator.GenerateDicomFile(
               studyInstanceUid,
               seriesInstanceUid,
               sopInstanceUid,
               null,
               rows,
               columns,
               TestFileBitDepth.EightBit,
               transferSyntax,
               encode,
               frames,
               photometricInterpretation);

            return dicomFile;
        }

        /// <summary>
        /// Will generate a file with valid 16bit pixel data representing a monochrome pattern.
        /// </summary>
        /// <param name="studyInstanceUid">Study UID (will generate new if null)</param>
        /// <param name="seriesInstanceUid">Series UID (will generate new if null)</param>
        /// <param name="sopInstanceUid">Instance UID (will generate new if null)</param>
        /// <param name="rows">width</param>
        /// <param name="columns">height</param>
        /// <param name="transferSyntax">Transfer Syntax</param>
        /// <param name="encode">Whether to encode image according to the supplied transfer syntax. False might result in invalid images</param>
        /// <param name="frames">Number of frames to generate</param>
        /// <param name="photometricInterpretation">Photometric Interpretation to be set on generated file</param>
        /// <returns>DicomFile</returns>
        public static DicomFile CreateRandomDicomFileWith16BitPixelData(
            string studyInstanceUid = null,
            string seriesInstanceUid = null,
            string sopInstanceUid = null,
            int rows = 512,
            int columns = 512,
            string transferSyntax = "1.2.840.10008.1.2.1", // Explicit VR Little Endian
            bool encode = true,
            int frames = 1,
            string photometricInterpretation = null)
        {
            DicomFile dicomFile = DicomImageGenerator.GenerateDicomFile(
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid,
                null,
                rows,
                columns,
                TestFileBitDepth.SixteenBit,
                transferSyntax,
                encode,
                frames,
                photometricInterpretation);

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
        string studyInstanceUid = null,
        string seriesInstanceUid = null,
        string sopInstanceUid = null,
        int rows = 50,
        int columns = 50,
        int frames = 1)
        {
            var result = new DicomFile(CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            AppendRandomPixelData(rows, columns, frames, result);
            return result;
        }

        public static DicomFile CreateRandomDicomFile(
                    string studyInstanceUid = null,
                    string seriesInstanceUid = null,
                    string sopInstanceUid = null,
                    bool validateItems = true)
        {
            return new DicomFile(CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid, validateItems: validateItems));
        }

        public static DicomFile CreateRandomDicomFileWithInvalidVr(
                   string studyInstanceUid = null,
                   string seriesInstanceUid = null,
                   string sopInstanceUid = null)
        {
            DicomFile file = new DicomFile(CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid, validateItems: false));
            file.Dataset.Add(GenerateNewDataSetWithInvalidVr());
            return file;
        }

        private static DicomDataset GenerateNewDataSetWithInvalidVr()
        {
            var dicomDataset = new DicomDataset().NotValidated();

            // CS VR type, char length should be less than or equal to 16
            dicomDataset.Add(DicomTag.Modality, "123456789ABCDEFGHIJK");

            return dicomDataset;
        }

        public static DicomDataset CreateRandomInstanceDataset(
            string studyInstanceUid = null,
            string seriesInstanceUid = null,
            string sopInstanceUid = null,
            string sopClassUid = null,
            bool validateItems = true)
        {
            var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian);

            if (!validateItems)
            {
                ds = ds.NotValidated();
            }

            ds.Add(DicomTag.StudyInstanceUID, studyInstanceUid ?? TestUidGenerator.Generate());
            ds.Add(DicomTag.SeriesInstanceUID, seriesInstanceUid ?? TestUidGenerator.Generate());
            ds.Add(DicomTag.SOPInstanceUID, sopInstanceUid ?? TestUidGenerator.Generate());
            ds.Add(DicomTag.SOPClassUID, sopClassUid ?? TestUidGenerator.Generate());
            ds.Add(DicomTag.BitsAllocated, (ushort)8);
            ds.Add(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value);
            ds.Add(DicomTag.PatientID, TestUidGenerator.Generate());
            return ds;
        }

        private static IByteBuffer CreateRandomPixelData(int pixelDataSize)
        {
            var result = new byte[pixelDataSize];
            for (var i = 0; i < pixelDataSize; i++)
            {
                result[i] = (byte)Rng.Next(0, 255);
            }

            return new MemoryByteBuffer(result);
        }
    }
}
