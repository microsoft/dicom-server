// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dicom;
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

        public static void AppendRandomPixelData(int rows, int columns, params DicomFile[] dicomFiles)
        {
            EnsureArg.IsGte(rows, 0, nameof(rows));
            EnsureArg.IsGte(columns, 0, nameof(columns));
            EnsureArg.IsNotNull(dicomFiles, nameof(dicomFiles));

            var random = new Random();
            var pixelDataSize = rows * columns;
            var result = new byte[pixelDataSize];

            for (var i = 0; i < pixelDataSize; i++)
            {
                result[i] = (byte)random.Next(0, 255);
            }

            dicomFiles.Each(x =>
            {
                x.Dataset.Add(DicomTag.PixelData, result);
                x.Dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)8);
                x.Dataset.AddOrUpdate(DicomTag.Rows, (ushort)rows);
                x.Dataset.AddOrUpdate(DicomTag.Columns, (ushort)columns);
            });
        }

        public static DicomFile CreateRandomDicomFileWithPixelData(
            string studyInstanceUID = null,
            string seriesInstanceUID = null,
            string sopInstanceUID = null,
            int rows = 512,
            int columns = 512)
        {
            var result = new DicomFile(CreateRandomInstanceDataset(studyInstanceUID, seriesInstanceUID, sopInstanceUID));
            AppendRandomPixelData(rows, columns, result);
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
            return new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, studyInstanceUID ?? Guid.NewGuid().ToString() },
                { DicomTag.SeriesInstanceUID, seriesInstanceUID ?? Guid.NewGuid().ToString() },
                { DicomTag.SOPInstanceUID, sopInstanceUID ?? Guid.NewGuid().ToString() },
                { DicomTag.SOPClassUID, sopClassUID ?? Guid.NewGuid().ToString() },
            };
        }
    }
}
