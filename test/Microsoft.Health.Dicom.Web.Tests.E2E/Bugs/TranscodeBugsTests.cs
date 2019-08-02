// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging.Codec;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;
using DicomFile = Dicom.DicomFile;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Bugs
{
    public class TranscodeBugsTests
    {
        [Fact]
        public async Task GivenValidJpegFile_WhenTranscodeIsCalled_CorruptedFileIsGenerated()
        {
            var tsFrom = DicomTransferSyntax.JPEGProcess1;
            var tsTo = DicomTransferSyntax.ExplicitVRLittleEndian;

            var filename = "transcodes\\JPEGProcess1.dcm";
            var studyInstanceUID = DicomUID.Generate().UID;
            var seriesInstanceUID = DicomUID.Generate().UID;
            var sopInstanceUID = DicomUID.Generate().UID;
            var sopClassUID = "1.2.840.10008.5.1.4.1.1.1";

            var dicomFile = Generator.GenerateDicomFile(
                studyInstanceUID,
                seriesInstanceUID,
                sopInstanceUID,
                sopClassUID,
                tsFrom.UID.UID);

            await dicomFile.SaveAsync(filename);

            dicomFile = DicomFile.Open(filename);

            Assert.Equal(dicomFile.Dataset.InternalTransferSyntax, tsFrom);

            var transcoder = new DicomTranscoder(
                dicomFile.Dataset.InternalTransferSyntax,
                tsTo);

            dicomFile = transcoder.Transcode(dicomFile);
            await dicomFile.SaveAsync("transcodes\\JPEGProcess1-ExplicitVRLittleEndian.dcm");

            Assert.Equal(dicomFile.Dataset.InternalTransferSyntax, tsTo);
            Assert.Equal("MONOCHROME2", dicomFile.Dataset.GetSingleValue<string>(DicomTag.PhotometricInterpretation));
        }
    }
}
