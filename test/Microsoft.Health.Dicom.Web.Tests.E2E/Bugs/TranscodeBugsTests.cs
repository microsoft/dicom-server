// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;
using Xunit.Abstractions;
using DicomFile = Dicom.DicomFile;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Bugs
{
    public class TranscodeBugsTests
    {
        private readonly ITestOutputHelper output;

        public TranscodeBugsTests(ITestOutputHelper output)
        {
            this.output = output;
        }

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

            var dicomFile = DicomImageGenerator.GenerateDicomFile(
                studyInstanceUID,
                seriesInstanceUID,
                sopInstanceUID,
                sopClassUID,
                512,
                512,
                TestFileBitDepth.EightBit,
                tsFrom.UID.UID);

            await dicomFile.SaveAsync(filename);

            dicomFile = DicomFile.Open(filename);

            Assert.Equal(dicomFile.Dataset.InternalTransferSyntax, tsFrom);

            var transcoder = new DicomTranscoder(
                dicomFile.Dataset.InternalTransferSyntax,
                tsTo);

            dicomFile = transcoder.Transcode(dicomFile);
            dicomFile.Dataset.AddOrUpdate(
                DicomTag.PhotometricInterpretation,
                PhotometricInterpretation.Monochrome2.Value);

            await dicomFile.SaveAsync("transcodes\\JPEGProcess1-ExplicitVRLittleEndian.dcm");

            Assert.Equal(dicomFile.Dataset.InternalTransferSyntax, tsTo);
            Assert.Equal("MONOCHROME2", dicomFile.Dataset.GetSingleValue<string>(DicomTag.PhotometricInterpretation));
        }

        [Fact]
        public async Task GenerateRandom8BitSamples()
        {
            var fromList = new List<string>
            {
                "DeflatedExplicitVRLittleEndian", "ExplicitVRBigEndian", "ExplicitVRLittleEndian", "ImplicitVRLittleEndian",
                "JPEG2000Lossless", "JPEG2000Lossy", "JPEGProcess1", "JPEGProcess2_4", "RLELossless",
            };

            var fromTsList = fromList.Select(x =>
                (name: x, transferSyntax: (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(x).GetValue(null)));

            var filesGenerated = 0;

            foreach (var ts in fromTsList)
            {
                try
                {
                    var dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(transferSyntax: ts.transferSyntax.UID.UID);
                    await dicomFile.SaveAsync($"genFiles/{ts.name}.dcm");

                    filesGenerated++;
                }
                catch (Exception e)
                {
                    output.WriteLine(e.ToString());
                }
            }

            Assert.Equal(fromList.Count, filesGenerated);
        }

        [Fact]
        public async Task GenerateRandom16BitSamples()
        {
            var fromList = new List<string>
            {
                "DeflatedExplicitVRLittleEndian", "ExplicitVRBigEndian", "ExplicitVRLittleEndian", "ImplicitVRLittleEndian",
                "JPEG2000Lossless", "JPEG2000Lossy", "JPEGProcess1", "JPEGProcess2_4", "RLELossless",
            };
            var fromTsList = fromList.Select(x =>
                (name: x, transferSyntax: (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(x).GetValue(null)));

            var filesGenerated = 0;

            foreach (var ts in fromTsList)
            {
                try
                {
                    var dicomFile = Samples.CreateRandomDicomFileWith16BitPixelData(transferSyntax: ts.transferSyntax.UID.UID);
                    await dicomFile.SaveAsync($"genFiles16/{ts.name}.dcm");

                    filesGenerated++;
                }
                catch (Exception e)
                {
                    output.WriteLine(e.ToString());
                }
            }

            Assert.Equal(fromList.Count, filesGenerated);
        }
    }
}
