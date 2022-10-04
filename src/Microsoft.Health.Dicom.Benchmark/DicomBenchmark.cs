// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom.Imaging;
using FellowOakDicom;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Health.Dicom.Benchmark;

public class DicomBenchmark
{
    protected DicomBenchmark()
    {
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
    }

    public IConfiguration Configuration { get; }

    protected static DicomDataset CreateRandomInstanceDataset(
        string studyInstanceUid = null,
        string seriesInstanceUid = null,
        string sopInstanceUid = null,
        string sopClassUid = null,
        DicomTransferSyntax dicomTransferSyntax = null)
    {
        var ds = new DicomDataset(dicomTransferSyntax ?? DicomTransferSyntax.ExplicitVRLittleEndian);
        ds = ds.NotValidated();

        ds.Add(DicomTag.StudyInstanceUID, studyInstanceUid ?? DicomUID.Generate().UID);
        ds.Add(DicomTag.SeriesInstanceUID, seriesInstanceUid ?? DicomUID.Generate().UID);
        ds.Add(DicomTag.SOPInstanceUID, sopInstanceUid ?? DicomUID.Generate().UID);
        ds.Add(DicomTag.SOPClassUID, sopClassUid ?? DicomUID.Generate().UID);
        ds.Add(DicomTag.BitsAllocated, (ushort)8);
        ds.Add(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value);
        ds.Add(DicomTag.PatientID, DicomUID.Generate().UID);

        return ds;
    }
}
