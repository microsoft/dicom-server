// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dicom;

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
            var dicomFile = DicomImageGenerator.GenerateDicomFile(
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
            var dicomFile = DicomImageGenerator.GenerateDicomFile(
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
                { DicomTag.StudyInstanceUID, studyInstanceUID ?? DicomUID.Generate().UID },
                { DicomTag.SeriesInstanceUID, seriesInstanceUID ?? DicomUID.Generate().UID },
                { DicomTag.SOPInstanceUID, sopInstanceUID ?? DicomUID.Generate().UID },
                { DicomTag.SOPClassUID, sopClassUID ?? DicomUID.Generate().UID },
            };

            return ds;
        }
    }
}
