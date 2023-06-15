// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------



using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve;
public class RetrieveHelpers
{
    public static DicomDataset GenerateDatasetsFromIdentifiers(InstanceIdentifier instanceIdentifier, string transferSyntaxUid = null)
    {
        DicomTransferSyntax syntax = DicomTransferSyntax.ExplicitVRLittleEndian;
        if (transferSyntaxUid != null)
        {
            syntax = DicomTransferSyntax.Parse(transferSyntaxUid);
        }

        var ds = new DicomDataset(syntax)
        {
            { DicomTag.StudyInstanceUID, instanceIdentifier.StudyInstanceUid },
            { DicomTag.SeriesInstanceUID, instanceIdentifier.SeriesInstanceUid },
            { DicomTag.SOPInstanceUID, instanceIdentifier.SopInstanceUid },
            { DicomTag.SOPClassUID, TestUidGenerator.Generate() },
            { DicomTag.PatientID, TestUidGenerator.Generate() },
            { DicomTag.BitsAllocated, (ushort)8 },
            { DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value },
        };

        return ds;
    }

    public static async Task<KeyValuePair<DicomFile, Stream>> StreamAndStoredFileFromDataset(DicomDataset dataset, RecyclableMemoryStreamManager recyclableMemoryStreamManager, int rows = 5, int columns = 5, int frames = 0, bool disposeStreams = false)
    {
        // Create DicomFile associated with input dataset with random pixel data.
        var dicomFile = new DicomFile(dataset);
        Samples.AppendRandomPixelData(rows, columns, frames, dicomFile);

        if (disposeStreams)
        {
            using MemoryStream disposableStream = recyclableMemoryStreamManager.GetStream();

            // Save file to a stream and reset position to 0.
            await dicomFile.SaveAsync(disposableStream);
            disposableStream.Position = 0;

            return new KeyValuePair<DicomFile, Stream>(dicomFile, disposableStream);
        }

        MemoryStream stream = recyclableMemoryStreamManager.GetStream();
        await dicomFile.SaveAsync(stream);
        stream.Position = 0;

        return new KeyValuePair<DicomFile, Stream>(dicomFile, stream);
    }

}
