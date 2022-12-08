// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;

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

        var numberOfStudies = new Option<int>(
            "--studies",
            description: "Number of studies to create, default is 1",
            getDefaultValue: () => 1);
        numberOfStudies.AddValidator((OptionResult result) =>
        {
            if (result.GetValueForOption(numberOfStudies) < 0 || result.GetValueForOption(numberOfStudies) >= 50)
            {
                result.ErrorMessage = "The value of --studies must not be less than 1 or more than 50.";
            }
        });


        var numberOfSeries = new Option<int>(
            "--series",
            description: "Number of series to create per study, default is 1",
            getDefaultValue: () => 1);
        numberOfSeries.AddValidator((OptionResult result) =>
        {
            if (result.GetValueForOption(numberOfSeries) < 0 || result.GetValueForOption(numberOfSeries) > 100)
            {
                result.ErrorMessage = "The value of --series must not be less than 1 or more than 100.";
            }
        });

        var numberOfInstances = new Option<int>(
            "--instances",
            description: "Number of instances to create per series, default is 1",
            getDefaultValue: () => 1);
        numberOfInstances.AddValidator((OptionResult result) =>
        {
            if (result.GetValueForOption(numberOfInstances) < 0 || result.GetValueForOption(numberOfInstances) > 1000)
            {
                result.ErrorMessage = "The value of --instances must not be less than 1 or more than 1000.";
            }
        });

        var rootCommand = new RootCommand("Generate one or optionally many DICOM file(s).");

        rootCommand.AddOption(filePath);
        rootCommand.AddOption(largeFile);
        rootCommand.AddOption(invalidSS);
        rootCommand.AddOption(invalidDS);
        rootCommand.AddOption(numberOfStudies);
        rootCommand.AddOption(numberOfSeries);
        rootCommand.AddOption(numberOfInstances);

        rootCommand.SetHandler(StoreImageAsync, filePath, largeFile, invalidSS, invalidDS, numberOfStudies, numberOfSeries, numberOfInstances);
        rootCommand.Invoke(args);
    }

    private static void StoreImageAsync(string filePath, bool largeFile, bool invalidSS, bool invalidDS, int numberOfStudies = 1, int numberOfSeries = 1, int numberOfInstances = 1)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            filePath += "\\";
        }

        var studyUids = Enumerable.Range(1, numberOfStudies).Select(_ => DicomUIDGenerator.GenerateDerivedFromUUID()).ToList();
        var seriesUids = Enumerable.Range(1, numberOfSeries).Select(_ => DicomUIDGenerator.GenerateDerivedFromUUID()).ToList();
        var instanceUids = Enumerable.Range(1, numberOfInstances).ToList();

        studyUids.ForEach(
            (studyUid) => seriesUids.ForEach(
                (seriesUid) => instanceUids.ForEach(
                    _ =>
                    {
                        var instanceUid = DicomUIDGenerator.GenerateDerivedFromUUID();

                        var file = GenerateDicomFile(
                            studyUid,
                            seriesUid,
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
                    )));
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
