// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Features.FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Workitem;

namespace Microsoft.Health.Dicom.Tests.Common;

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
        int frames = 1,
        DicomTransferSyntax dicomTransferSyntax = null)
    {
        var result = new DicomFile(CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTransferSyntax: dicomTransferSyntax));
        AppendRandomPixelData(rows, columns, frames, result);
        return result;
    }

    public static DicomFile CreateRandomDicomFileWithFragmentPixelData(
        string studyInstanceUid = null,
        string seriesInstanceUid = null,
        string sopInstanceUid = null,
        int rows = 50,
        int columns = 50,
        int frames = 1,
        DicomTransferSyntax dicomTransferSyntax = null)
    {
        var result = new DicomFile(CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTransferSyntax: dicomTransferSyntax));
        var pixelDataSize = rows * columns;
        const ushort bitsAllocated = 8;
        result.Dataset.AddOrUpdate(DicomTag.Rows, (ushort)rows);
        result.Dataset.AddOrUpdate(DicomTag.Columns, (ushort)columns);
        result.Dataset.AddOrUpdate(DicomTag.BitsAllocated, bitsAllocated);

        var pixelData = new DicomOtherByteFragment(DicomTag.PixelData);
        for (int i = 0; i < frames; i++)
        {
            pixelData.Fragments.Add(CreateRandomPixelData(pixelDataSize));
        }
        result.Dataset.AddOrUpdate(pixelData);

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
        bool validateItems = true,
        DicomTransferSyntax dicomTransferSyntax = null,
        string patientId = null)
    {
        var ds = new DicomDataset(dicomTransferSyntax ?? DicomTransferSyntax.ExplicitVRLittleEndian);

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
        ds.Add(DicomTag.PatientID, patientId ?? TestUidGenerator.Generate());
        ds.Add(DicomTag.PatientName, TestUidGenerator.Generate());
        return ds;
    }

    public static DicomDataset CreateRandomWorkitemInstanceDataset(
        string workitemUid = null,
        bool validateItems = true,
        DicomTransferSyntax dicomTransferSyntax = null,
        WorkitemRequestType requestType = WorkitemRequestType.Add)
    {
        var ds = new DicomDataset(dicomTransferSyntax ?? DicomTransferSyntax.ExplicitVRLittleEndian);

        if (!validateItems)
        {
            ds = ds.NotValidated();
        }

        ds.Add(DicomTag.TransactionUID, string.Empty);

        if (requestType == WorkitemRequestType.Add)
        {
            ds.Add(DicomTag.SOPClassUID, TestUidGenerator.Generate());
            ds.Add(DicomTag.SOPInstanceUID, workitemUid ?? TestUidGenerator.Generate());
            ds.Add(new DicomSequence(DicomTag.ScheduledProcessingParametersSequence));
            ds.Add(new DicomSequence(DicomTag.ScheduledStationNameCodeSequence));
            ds.Add(new DicomSequence(DicomTag.ScheduledStationClassCodeSequence));
            ds.Add(new DicomSequence(DicomTag.ScheduledStationGeographicLocationCodeSequence));
            ds.Add(new DicomSequence(DicomTag.ScheduledWorkitemCodeSequence));
            ds.Add(new DicomSequence(DicomTag.IssuerOfPatientIDQualifiersSequence));
            ds.Add(DicomTag.IssuerOfAdmissionIDSequence, new DicomDataset());
            ds.Add(DicomTag.AdmittingDiagnosesCodeSequence, new DicomDataset
            {
                { DicomTag.CodeMeaning, "CodeMeaning" }
            });
            ds.Add(new DicomSequence(DicomTag.ReferencedRequestSequence));
            ds.Add(new DicomSequence(DicomTag.InputInformationSequence));
            ds.Add(DicomTag.PatientName, string.Empty);
            ds.Add(DicomTag.IssuerOfPatientID, string.Empty);
            ds.Add(new DicomSequence(DicomTag.OtherPatientIDsSequence));
            ds.Add(DicomTag.PatientBirthDate, DateTime.Now);
            ds.Add(DicomTag.PatientSex, "F");
            ds.Add(DicomTag.AdmissionID, "1");
            ds.Add(DicomTag.AdmittingDiagnosesDescription, "SampleDiagnosesDescription");
            ds.Add(DicomTag.ProcedureStepState, "SCHEDULED");
            ds.Add(new DicomSequence(DicomTag.ProcedureStepProgressInformationSequence));
            ds.Add(new DicomSequence(DicomTag.UnifiedProcedureStepPerformedProcedureSequence));
        }

        ds.Add(DicomTag.ScheduledProcedureStepModificationDateTime, DateTime.Now);
        ds.Add(DicomTag.ScheduledProcedureStepPriority, Guid.NewGuid().ToString("N").Substring(0, 14).ToUpper());
        ds.Add(DicomTag.ProcedureStepLabel, Guid.NewGuid().ToString("N"));
        ds.Add(DicomTag.WorklistLabel, "WorklistLabel");
        ds.Add(DicomTag.ScheduledProcedureStepStartDateTime, DateTime.Now);
        ds.Add(DicomTag.CommentsOnTheScheduledProcedureStep, "SampleComments");
        ds.Add(DicomTag.InputReadinessState, Guid.NewGuid().ToString("N").Substring(0, 14).ToUpper());
        ds.Add(new DicomSequence(DicomTag.ProcedureStepDiscontinuationReasonCodeSequence));
        ds.Add(new DicomSequence(DicomTag.ActualHumanPerformersSequence));
        ds.Add(new DicomSequence(DicomTag.HumanPerformerCodeSequence));
        ds.Add(DicomTag.HumanPerformerName, "SampleFixtureUser");
        ds.Add(DicomTag.TypeOfInstances, "SAMPLETYPEOFINST");
        ds.Add(DicomTag.ReferencedSOPSequence, new DicomDataset
        {
            { DicomTag.ReferencedSOPClassUID, "1.2.3" },
            { DicomTag.ReferencedSOPInstanceUID, "1.2.3" },
        });

        return ds;
    }

    public static DicomDataset CreateWorkitemCancelRequestDataset(string cancellationReason)
    {
        var cancelRequestDataset = new DicomDataset
        {
            { DicomTag.ReasonForCancellation, cancellationReason },
            { DicomTag.ContactURI, @"dicom-users://test-user" },
            { DicomTag.ContactDisplayName, @"Dicom Test User" },
            { DicomTag.ProcedureStepCancellationDateTime, DateTime.UtcNow },

            // This is needed to show progress on the workitem before canceling it.
            new DicomSequence(DicomTag.UnifiedProcedureStepPerformedProcedureSequence, new DicomDataset
            {
                new DicomSequence(DicomTag.ActualHumanPerformersSequence, new DicomDataset
                    {
                        new DicomSequence(DicomTag.HumanPerformerCodeSequence, new DicomDataset()),
                        { DicomTag.HumanPerformerName, @"Dicom Test User" }
                    }),
                new DicomSequence(DicomTag.PerformedStationNameCodeSequence, new DicomDataset()),
                { DicomTag.PerformedProcedureStepStartDateTime, DateTime.UtcNow},
                new DicomSequence(DicomTag.PerformedWorkitemCodeSequence, new DicomDataset()),
                { DicomTag.PerformedProcedureStepEndDateTime, DateTime.UtcNow + TimeSpan.FromDays(2)},
                new DicomSequence(DicomTag.OutputInformationSequence, new DicomDataset()),
            })
        };

        return cancelRequestDataset;
    }

    public static DicomDataset CreateCanceledWorkitemDataset(
        string cancellationReason,
        ProcedureStepState procedureStepState = ProcedureStepState.Scheduled)
    {
        var uid = TestUidGenerator.Generate();

        var cancelRequestDataset = new DicomDataset
        {
            { DicomTag.ReasonForCancellation, cancellationReason },
            { DicomTag.ContactURI, @"dicom-users://test-user" },
            { DicomTag.ContactDisplayName, @"Dicom Test User" },
        };
        cancelRequestDataset.AddOrUpdate(DicomTag.ProcedureStepDiscontinuationReasonCodeSequence, new DicomDataset
        {
            { DicomTag.ReasonForCancellation, cancellationReason }
        });

        var dataset = CreateRandomWorkitemInstanceDataset(uid);

        dataset.AddOrUpdate(DicomTag.SOPClassUID, TestUidGenerator.Generate());
        dataset.AddOrUpdate(DicomTag.SOPInstanceUID, uid);

        // Unified Procedure Step Scheduled Procedure Information Module
        dataset.AddOrUpdate(DicomTag.ScheduledProcedureStepPriority, Guid.NewGuid().ToString("N").Substring(0, 14).ToUpper());
        dataset.AddOrUpdate(DicomTag.ScheduledProcedureStepModificationDateTime, DateTime.UtcNow);
        dataset.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, DateTime.UtcNow);
        dataset.AddOrUpdate(DicomTag.InputReadinessState, Guid.NewGuid().ToString("N").Substring(0, 14).ToUpper());

        dataset.AddOrUpdate(DicomTag.ProcedureStepState, procedureStepState.GetStringValue());
        dataset.AddOrUpdate(DicomTag.ProcedureStepProgressInformationSequence, new DicomDataset
        {
            { DicomTag.ProcedureStepCancellationDateTime, DateTime.UtcNow },
            new DicomSequence(DicomTag.ProcedureStepDiscontinuationReasonCodeSequence, new DicomDataset
            {
                { DicomTag.ReasonForCancellation, cancellationReason }
            })
        });

        // Unified Procedure Step Performed Procedure Information Module
        dataset.AddOrUpdate(DicomTag.UnifiedProcedureStepPerformedProcedureSequence, new DicomDataset
        {
            new DicomSequence(DicomTag.ActualHumanPerformersSequence, new DicomDataset
            {
                new DicomSequence(DicomTag.HumanPerformerCodeSequence, new DicomDataset()),
                { DicomTag.HumanPerformerName, @"Samples-TestFixture" }
            }),
            new DicomSequence(DicomTag.PerformedStationNameCodeSequence, new DicomDataset()),
            { DicomTag.PerformedProcedureStepStartDateTime, DateTime.UtcNow },
            new DicomSequence(DicomTag.PerformedWorkitemCodeSequence, new DicomDataset()),
            { DicomTag.PerformedProcedureStepEndDateTime, DateTime.UtcNow },
            new DicomSequence(DicomTag.OutputInformationSequence, new DicomDataset())
        });

        return dataset;
    }

    public static (DicomUID classUID, string versionName) GetDicomImplemenationClasUIDAndVersionName()
    {
        Assembly assembly = typeof(CustomDicomImplementation).GetTypeInfo().Assembly;
        AssemblyFileVersionAttribute fileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
        string expectedVersion = fileVersionAttribute?.Version ?? "Unknown";
        var classUID = new DicomUID("1.3.6.1.4.1.311.129", "Implementation Class UID", DicomUidType.Unknown);

        return (classUID, expectedVersion);
    }

    private static IByteBuffer CreateRandomPixelData(int pixelDataSize)
    {
        return new StreamByteBuffer(new RandomStream(pixelDataSize), 0, pixelDataSize);
    }

    public static IEnumerable<DicomTransferSyntax> GetAllDicomTransferSyntax()
    {
        yield return DicomTransferSyntax.ExplicitVRLittleEndian;
        yield return DicomTransferSyntax.ExplicitVRBigEndian;
        yield return DicomTransferSyntax.DeflatedExplicitVRLittleEndian;
        yield return DicomTransferSyntax.JPEGProcess1;
        yield return DicomTransferSyntax.JPEGProcess2_4;
        yield return DicomTransferSyntax.JPEGProcess3_5Retired;
        yield return DicomTransferSyntax.JPEGProcess6_8Retired;
        yield return DicomTransferSyntax.JPEGProcess7_9Retired;
        yield return DicomTransferSyntax.JPEGProcess10_12Retired;
        yield return DicomTransferSyntax.JPEGProcess11_13Retired;
        yield return DicomTransferSyntax.JPEGProcess14;
        yield return DicomTransferSyntax.JPEGProcess15Retired;
        yield return DicomTransferSyntax.JPEGProcess16_18Retired;
        yield return DicomTransferSyntax.JPEGProcess17_19Retired;
        yield return DicomTransferSyntax.JPEGProcess20_22Retired;
        yield return DicomTransferSyntax.JPEGProcess21_23Retired;
        yield return DicomTransferSyntax.JPEGProcess24_26Retired;
        yield return DicomTransferSyntax.JPEGProcess25_27Retired;
        yield return DicomTransferSyntax.JPEGProcess28Retired;
        yield return DicomTransferSyntax.JPEGProcess29Retired;
        yield return DicomTransferSyntax.JPEGProcess14SV1;
        yield return DicomTransferSyntax.JPEGLSLossless;
        yield return DicomTransferSyntax.JPEGLSNearLossless;
        yield return DicomTransferSyntax.JPEG2000Lossless;
        yield return DicomTransferSyntax.JPEG2000Lossy;
        yield return DicomTransferSyntax.JPEG2000Part2MultiComponentLosslessOnly;
        yield return DicomTransferSyntax.JPEG2000Part2MultiComponent;
        yield return DicomTransferSyntax.JPIPReferenced;
        yield return DicomTransferSyntax.JPIPReferencedDeflate;
        yield return DicomTransferSyntax.MPEG2;
        yield return DicomTransferSyntax.MPEG2MainProfileHighLevel;
        yield return DicomTransferSyntax.MPEG4AVCH264HighProfileLevel41;
        yield return DicomTransferSyntax.MPEG4AVCH264BDCompatibleHighProfileLevel41;
        yield return DicomTransferSyntax.MPEG4AVCH264HighProfileLevel42For2DVideo;
        yield return DicomTransferSyntax.MPEG4AVCH264HighProfileLevel42For3DVideo;
        yield return DicomTransferSyntax.MPEG4AVCH264StereoHighProfileLevel42;
        yield return DicomTransferSyntax.HEVCH265MainProfileLevel51;
        yield return DicomTransferSyntax.HEVCH265Main10ProfileLevel51;
        yield return DicomTransferSyntax.RLELossless;
        yield return DicomTransferSyntax.RFC2557MIMEEncapsulation;
        yield return DicomTransferSyntax.XMLEncoding;
        yield return DicomTransferSyntax.ImplicitVRBigEndian;
        yield return DicomTransferSyntax.ImplicitVRLittleEndian;
        yield return DicomTransferSyntax.Papyrus3ImplicitVRLittleEndianRetired;
        yield return DicomTransferSyntax.GEPrivateImplicitVRBigEndian;
        yield return DicomTransferSyntax.ImplicitVRBigEndian;
        yield return DicomTransferSyntax.ImplicitVRLittleEndian;
        yield return DicomTransferSyntax.Papyrus3ImplicitVRLittleEndianRetired;
        yield return DicomTransferSyntax.GEPrivateImplicitVRBigEndian;
    }
}
