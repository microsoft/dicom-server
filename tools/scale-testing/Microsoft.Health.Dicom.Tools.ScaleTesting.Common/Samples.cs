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

namespace Microsoft.Health.Dicom.Tools.ScaleTesting.Common
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

        public static DicomFile CreateRandomDicomFileWithPixelData(
            PatientInstance pI,
            int rows = 50,
            int columns = 50,
            int frames = 1)
        {
            var result = new DicomFile(CreateRandomInstanceDataset(pI));
            AppendRandomPixelData(rows, columns, frames, result);
            return result;
        }

        public static DicomFile CreateRandomDicomFile(
                    string studyInstanceUid = null,
                    string seriesInstanceUid = null,
                    string sopInstanceUid = null)
        {
            return new DicomFile(CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
        }

        public static DicomFile CreateRandomDicomFileWithInvalidVr(
                   string studyInstanceUid = null,
                   string seriesInstanceUid = null,
                   string sopInstanceUid = null)
        {
            DicomFile file = new DicomFile(CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid));

#pragma warning disable CS0618 // Type or member is obsolete
            DicomValidation.AutoValidation = false;
#pragma warning restore CS0618 // Type or member is obsolete

            file.Dataset.Add(GenerateNewDataSetWithInvalidVr());

#pragma warning disable CS0618 // Type or member is obsolete
            DicomValidation.AutoValidation = true;
#pragma warning restore CS0618 // Type or member is obsolete

            return file;
        }

        private static DicomDataset GenerateNewDataSetWithInvalidVr()
        {
            var dicomDataset = new DicomDataset();

            dicomDataset.Add(DicomTag.SeriesDescription, "CT1 abdomen\u0000");

            return dicomDataset;
        }

        public static DicomDataset CreateRandomInstanceDataset(
            string studyInstanceUid = null,
            string seriesInstanceUid = null,
            string sopInstanceUid = null,
            string sopClassUid = null)
        {
            var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
            {
                { DicomTag.StudyInstanceUID, studyInstanceUid ?? TestUidGenerator.Generate() },
                { DicomTag.SeriesInstanceUID, seriesInstanceUid ?? TestUidGenerator.Generate() },
                { DicomTag.SOPInstanceUID, sopInstanceUid ?? TestUidGenerator.Generate() },
                { DicomTag.SOPClassUID, sopClassUid ?? TestUidGenerator.Generate() },
                { DicomTag.BitsAllocated, (ushort)8 },
                { DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value },
                { DicomTag.PatientID, TestUidGenerator.Generate() },
            };

            return ds;
        }

        public static DicomDataset CreateRandomInstanceDataset(
            PatientInstance pI)
        {
            Random rand = new Random();
            var studyTime = DateTime.Parse(pI.PerformedProcedureStepStartDate).AddMinutes(-10);
            var dicomTime = studyTime.ToString("HHmmss.fffff");

            var procedureTime = DateTime.Parse(pI.PerformedProcedureStepStartDate);
            var procedureDate = studyTime.ToString("yyyyMMdd");
            var dicomProcedureTime = studyTime.ToString("HHmmss.fffff");

            var age = int.Parse(pI.PatientAge);
            string dicomAge = null;
            if (age > 9)
            {
                dicomAge = "0" + age.ToString() + "Y";
            }
            else
            {
                dicomAge = "00" + age.ToString() + "Y";
            }

            var occupation = pI.PatientOccupation.Length > 16 ? pI.PatientOccupation.Substring(0, 16) : pI.PatientOccupation;
            var laterality = new List<string> { "R", "L" };

            var physicianName = pI.PhysicianName.Split()[0] + "^" + pI.PhysicianName.Split()[1];
            var name = pI.Name.Split()[0] + "^" + pI.Name.Split()[1];

            var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
            {
                { DicomTag.StudyInstanceUID, pI.StudyUid ?? TestUidGenerator.Generate() },
                { DicomTag.SeriesInstanceUID, pI.SeriesUid ?? TestUidGenerator.Generate() },
                { DicomTag.SOPInstanceUID, pI.InstanceUid ?? TestUidGenerator.Generate() },
                { DicomTag.SOPClassUID, TestUidGenerator.Generate() },
                { DicomTag.BitsAllocated, (ushort)8 },
                { DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value },
                { DicomTag.PatientID, pI.PatientId ?? TestUidGenerator.Generate() },
                { DicomTag.SpecificCharacterSet,  "ISO_IR 100" },
                { DicomTag.StudyDate, procedureDate },
                { DicomTag.SeriesDate, procedureDate },
                { DicomTag.StudyTime,  dicomTime },
                { DicomTag.SeriesTime, dicomTime },
                { DicomTag.AccessionNumber, pI.AccessionNumber },
                { DicomTag.InstanceAvailability, "ONLINE" },
                { DicomTag.Modality, pI.Modality },
                { DicomTag.StudyDescription, pI.StudyDescription },
                { DicomTag.SeriesDescription, pI.StudyDescription },
                { DicomTag.NameOfPhysiciansReadingStudy, physicianName },
                { DicomTag.ReferringPhysicianName, physicianName },
                { DicomTag.PatientName, name },
                { DicomTag.PatientBirthDate, pI.PatientBirthDate },
                { DicomTag.PatientSex, pI.PatientSex },
                { DicomTag.PatientAge, dicomAge },
                { DicomTag.PatientWeight, pI.PatientWeight },
                { DicomTag.Occupation, occupation },
                { DicomTag.StudyID, rand.Next(10000000, 100000000).ToString() },
                { DicomTag.SeriesNumber, pI.SeriesIndex },
                { DicomTag.InstanceNumber, pI.InstanceIndex },
                { DicomTag.Laterality, laterality.RandomElement() },
                { DicomTag.PerformedProcedureStepStartDate, procedureDate },
                { DicomTag.PerformedProcedureStepStartTime, dicomProcedureTime },
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
