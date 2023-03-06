// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Text;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;

namespace Microsoft.Health.Dicom.FileGenerator;

public class Generator
{
    private const int PixelsPerMB = 520000;
    private readonly DicomDataset _generatedPixelData;

    public Generator(int fileSizeInMB = 1)
    {
        _generatedPixelData = GetPixelData(fileSizeInMB);
    }

    public void SaveFiles(string filePath, bool invalidSS, bool invalidDS, int numberOfStudies = 1, int numberOfSeries = 1, int numberOfInstances = 1)
    {
        if (!string.IsNullOrWhiteSpace(filePath) && !Path.EndsInDirectorySeparator(filePath))
        {
            filePath += Path.DirectorySeparatorChar;
        }

        var studyUids = Enumerable.Range(1, numberOfStudies).Select(_ => DicomUIDGenerator.GenerateDerivedFromUUID()).ToList();
        var seriesUids = Enumerable.Range(1, numberOfSeries).Select(_ => DicomUIDGenerator.GenerateDerivedFromUUID()).ToList();
        var instanceUids = Enumerable.Range(1, numberOfInstances).Select(_ => DicomUIDGenerator.GenerateDerivedFromUUID()).ToList();

        int instancesWritten = 0;

        studyUids.ForEach(
            (studyUid) => seriesUids.ForEach(
                (seriesUid) => instanceUids.ForEach(
                    (instanceUid) =>
                    {
                        var file = GenerateDicomFile(
                            studyUid,
                            seriesUid,
                            instanceUid);

                        if (invalidDS)
                        {
                            file.Dataset.Add(DicomTag.PatientWeight, "asdf");
                        }

                        if (invalidSS)
                        {
                            file.Dataset.Add(new DicomSignedShort(DicomTag.TagAngleSecondAxis, new MemoryByteBuffer(Encoding.UTF8.GetBytes("asdf"))));
                        }

                        var fullPath = $"{filePath}{studyUid.UID}-{seriesUid.UID}-{instanceUid.UID}.dcm";
                        file.Save(fullPath);
                        instancesWritten++;

                        if (instancesWritten % 50 == 0)
                        {
                            Console.WriteLine($"{instancesWritten} instances written to {filePath}");
                        }
                    }
                    )));
        Console.WriteLine($"{instancesWritten} instances written to {filePath}");
    }

    private DicomFile GenerateDicomFile(
        DicomUID studyInstanceUid,
        DicomUID seriesInstanceUid,
        DicomUID sopInstanceUid)
    {
        var dataset = _generatedPixelData.Clone();

        dataset.AddOrUpdate(DicomTag.StudyInstanceUID, studyInstanceUid ?? DicomUIDGenerator.GenerateDerivedFromUUID());
        dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, seriesInstanceUid ?? DicomUIDGenerator.GenerateDerivedFromUUID());
        dataset.AddOrUpdate(DicomTag.SOPInstanceUID, sopInstanceUid ?? DicomUIDGenerator.GenerateDerivedFromUUID());
        dataset.Add(DicomTag.SOPClassUID, DicomUID.UltrasoundMultiFrameImageStorage);
        dataset.Add(DicomTag.PatientName, "Patient Name");
        dataset.Add(DicomTag.PatientID, "12345");
        dataset.Add(DicomTag.PatientBirthDate, DateTime.Now);
        dataset.Add(DicomTag.AccessionNumber, "Accession");
        dataset.Add(DicomTag.ReferringPhysicianName, "Physician Name");
        dataset.Add(DicomTag.StudyDate, DateTime.Now);
        dataset.Add(DicomTag.StudyDescription, "Study Description");
        dataset.Add(DicomTag.Modality, "US");
        dataset.Add(DicomTag.PerformedProcedureStepStartDate, DateTime.Now);
        dataset.Add(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value);
        dataset.Add(DicomTag.WindowWidth, 65536m);
        dataset.Add(DicomTag.WindowCenter, 32768m);

        return new DicomFile(dataset);
    }

    private static DicomDataset GetPixelData(int fileSizeInMB)
    {
        var height = (int)Math.Sqrt(fileSizeInMB * PixelsPerMB);
        var rows = height;
        var cols = height;
        var frames = 1;
        var bitDepth = (ushort)16;

        var dataset = new DicomDataset();
#pragma warning disable CS0618 // We want to generate invalid data for testing
        dataset.AutoValidate = false;
#pragma warning restore CS0618

        dataset.Add(DicomTag.Rows, (ushort)rows);
        dataset.Add(DicomTag.Columns, (ushort)cols);
        dataset.Add(DicomTag.BitsAllocated, bitDepth);

        var pixelData = DicomPixelData.Create(dataset, true);
        pixelData.SamplesPerPixel = 1;
        pixelData.BitsStored = bitDepth;
        pixelData.HighBit = (ushort)(bitDepth - 1);
        pixelData.PixelRepresentation = PixelRepresentation.Unsigned;

        for (int i = 0; i < frames; i++)
        {
            var buffer = new MemoryByteBuffer(GetBytesFor16BitImage(rows, cols, i));

            pixelData.AddFrame(buffer);
        }

        return dataset;
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
}
