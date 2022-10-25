// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.CommandLine;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using FellowOakDicom;
using System.Text;

namespace Microsoft.Health.Web.Dicom.Tool;

public static class Program
{
    public static void Main(string[] args)
    {
        ParseArgumentsAndExecute(args);
    }

    private static void ParseArgumentsAndExecute(string[] args)
    {
        var filePath = new Option<string>(
                    "--filePath",
                    description: "Path to file, e.g.: \"C:\\dicomdir\"");

        var largeFile = new Option<bool>(
                    "--largeFile",
                    description: "Create a file larger than 2GB");

        var invalidSS = new Option<bool>(
                    "--invalidSS",
                    description: "Add an invalid Signed Short attribute");

        var invalidDS = new Option<bool>(
                    "--invalidDS",
                    description: "Add an invalid Decimal String attribute");

        var rootCommand = new RootCommand("Generate a DICOM file.");

        rootCommand.AddOption(filePath);
        rootCommand.AddOption(largeFile);
        rootCommand.AddOption(invalidSS);
        rootCommand.AddOption(invalidDS);

        rootCommand.SetHandler(StoreImageAsync, filePath, largeFile, invalidSS, invalidDS);
        rootCommand.Invoke(args);
    }

    private static void StoreImageAsync(string filePath, bool largeFile, bool invalidSS, bool invalidDS)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            filePath += "\\";
        }

        var instanceUid = DicomUIDGenerator.GenerateDerivedFromUUID();

        var file = GenerateDicomFile(
            DicomUIDGenerator.GenerateDerivedFromUUID(),
            DicomUIDGenerator.GenerateDerivedFromUUID(),
            instanceUid,
            DicomUIDGenerator.GenerateDerivedFromUUID(),
            largeFile);

        if (invalidDS)
        {
            file.Dataset.Add(DicomTag.PatientWeight, "asdf");
        }

        if (invalidSS)
        {
            file.Dataset.Add(new DicomSignedShort(DicomTag.TagAngleSecondAxis, new MemoryByteBuffer(Encoding.UTF8.GetBytes("asdf"))));
        }

        file.Save($"{filePath}{instanceUid.UID}.dcm");

    }

    private static DicomFile GenerateDicomFile(
        DicomUID studyInstanceUid,
        DicomUID seriesInstanceUid,
        DicomUID sopInstanceUid,
        DicomUID sopClassUid,
        bool largeFile)
    {
        var dataset = new DicomDataset();
#pragma warning disable CS0618 // We want to generate invalid data for testing
        dataset.AutoValidate = false;
#pragma warning restore CS0618

        var rows = largeFile ? 10000 : 0;
        var cols = largeFile ? 10000 : 0;
        var frames = largeFile ? 500 : 0;

        var bitDepth = (ushort)16;

        dataset.Add(DicomTag.StudyInstanceUID, studyInstanceUid ?? DicomUIDGenerator.GenerateDerivedFromUUID());
        dataset.Add(DicomTag.SeriesInstanceUID, seriesInstanceUid ?? DicomUIDGenerator.GenerateDerivedFromUUID());
        dataset.Add(DicomTag.SOPInstanceUID, sopInstanceUid ?? DicomUIDGenerator.GenerateDerivedFromUUID());
        dataset.Add(DicomTag.SOPClassUID, sopClassUid ?? DicomUIDGenerator.GenerateDerivedFromUUID());
        dataset.Add(DicomTag.PatientName, "Patient Name");
        dataset.Add(DicomTag.PatientID, "12345");
        dataset.Add(DicomTag.PatientBirthDate, DateTime.Now);
        dataset.Add(DicomTag.AccessionNumber, "Accession");
        dataset.Add(DicomTag.ReferringPhysicianName, "Physician Name");
        dataset.Add(DicomTag.StudyDate, DateTime.Now);
        dataset.Add(DicomTag.StudyDescription, "Study Description");
        dataset.Add(DicomTag.Modality, "US");
        dataset.Add(DicomTag.PerformedProcedureStepStartDate, DateTime.Now);

        dataset.Add(DicomTag.Rows, (ushort)rows);
        dataset.Add(DicomTag.Columns, (ushort)cols);
        dataset.Add(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value);
        dataset.Add(DicomTag.BitsAllocated, bitDepth);
        dataset.Add(DicomTag.WindowWidth, 65536m);
        dataset.Add(DicomTag.WindowCenter, 32768m);

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

        return new DicomFile(dataset);
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
