// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging.Codec;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Rest;
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

        /// <summary>
        /// This test will start failing when a bug with JPEG transcoding is fixed in future
        /// versions of fo-dicom.
        /// </summary>
        [Fact]
        public async Task GivenValidJpegFile_WhenTranscodeIsCalled_CorruptedFileIsGenerated()
        {
            var tsFrom = DicomTransferSyntax.JPEGProcess1;
            var tsTo = DicomTransferSyntax.ExplicitVRLittleEndian;

            var dirName = "transcodings";
            var filename = Path.Combine(dirName, "JPEGProcess1.dcm");
            var studyInstanceUID = TestUidGenerator.Generate();
            var seriesInstanceUID = TestUidGenerator.Generate();
            var sopInstanceUID = TestUidGenerator.Generate();
            var sopClassUID = "1.2.840.10008.5.1.4.1.1.1";

            var dicomFile = DicomImageGenerator.GenerateDicomFile(
                studyInstanceUID,
                seriesInstanceUID,
                sopInstanceUID,
                sopClassUID,
                512,
                512,
                TestFileBitDepth.EightBit,
                tsFrom.UID.UID,
                true);

            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            await dicomFile.SaveAsync(filename);

            dicomFile = DicomFile.Open(filename);

            Assert.Equal(dicomFile.Dataset.InternalTransferSyntax, tsFrom);

            var transcoder = new DicomTranscoder(
                dicomFile.Dataset.InternalTransferSyntax,
                tsTo,
                outputCodecParams: new DicomJpegParams());

            dicomFile = transcoder.Transcode(dicomFile);

            // BUG! Without the following line, incorrect photometric interpretation is set
            // dicomFile.Dataset.AddOrUpdate(
            //    DicomTag.PhotometricInterpretation,
            //    PhotometricInterpretation.Monochrome2.Value);

            await dicomFile.SaveAsync(Path.Combine(dirName, "JPEGProcess1-ExplicitVRLittleEndian.dcm"));

            Assert.Equal(dicomFile.Dataset.InternalTransferSyntax, tsTo);

            // This is the correct assert if the lib worked as it should
            // Assert.Equal("MONOCHROME2", dicomFile.Dataset.GetSingleValue<string>(DicomTag.PhotometricInterpretation));

            // This is the bugged assert:
            Assert.Equal("RGB", dicomFile.Dataset.GetSingleValue<string>(DicomTag.PhotometricInterpretation));
        }

        /// <summary>
        /// Currently there is a bug with fo-dicom 4.0.1 where some transcodings fail and some produce garbage.
        /// This test will pass, but some files will be corrupted when examined in a viewer.
        /// </summary>
        [Fact]
        public async Task Gen_GivenValid8BitSampleData_WhenTranscodingRequested_ShouldConvertDataWithSupportedEncodings()
        {
            var dirName = "genFiles8bit";
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

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
                    await dicomFile.SaveAsync(Path.Combine(dirName, $"{ts.name}.dcm"));

                    filesGenerated++;
                }
                catch (Exception e)
                {
                    output.WriteLine(e.ToString());
                }
            }

            Assert.Equal(fromList.Count, filesGenerated);
        }

        /// <summary>
        /// Currently there is a bug with fo-dicom 4.0.1 where some transcodings will produce garbage. The test will pass
        /// but some images will be corrupted.
        /// </summary>
        [Fact]
        public async Task Gen_GivenValid16BitSampleData_WhenTranscodingRequested_ShouldConvertDataWithSupportedEncodings()
        {
            var dirName = "genFiles16bit";
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            var fromList = new List<string>
            {
                "DeflatedExplicitVRLittleEndian", "ExplicitVRBigEndian", "ExplicitVRLittleEndian", "ImplicitVRLittleEndian",
                "JPEG2000Lossless", "JPEG2000Lossy", "RLELossless",

                // "JPEGProcess1", "JPEGProcess2_4", <-- are not supported for 16bit data
            };
            var fromTsList = fromList.Select(x =>
                (name: x, transferSyntax: (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(x).GetValue(null)));

            var filesGenerated = 0;

            foreach (var ts in fromTsList)
            {
                try
                {
                    var dicomFile = Samples.CreateRandomDicomFileWith16BitPixelData(transferSyntax: ts.transferSyntax.UID.UID);
                    await dicomFile.SaveAsync(Path.Combine(dirName, $"{ts.name}.dcm"));

                    filesGenerated++;
                }
                catch (Exception e)
                {
                    output.WriteLine(e.ToString());
                }
            }

            Assert.Equal(fromList.Count, filesGenerated);
        }

        public static IEnumerable<object[]> Get8BitTranscoderCombos()
        {
            var fromList = RetrieveTransactionResourceTests.SupportedTransferSyntaxesFor8BitTranscoding;
            var toList = RetrieveTransactionResourceTests.SupportedTransferSyntaxesFor8BitTranscoding;

            return from x in fromList from y in toList select new[] { x, y };
        }

        public static IEnumerable<object[]> Get16BitTranscoderCombos()
        {
            var fromList = RetrieveTransactionResourceTests.SupportedTransferSyntaxesForOver8BitTranscoding;
            var toList = RetrieveTransactionResourceTests.SupportedTransferSyntaxesForOver8BitTranscoding;

            return from x in fromList from y in toList select new[] { x, y };
        }

        /// <summary>
        /// Test all possible transcodings between supported transfer syntaxes for a real US image (RGB).
        /// All of these convert without exceptions, but results for some conversions from JPEG2K
        /// are not quite right upon visual inspection
        /// </summary>
        /// <param name="tsFrom">Transfer syntax to convert from</param>
        /// <param name="tsTo">Transfer syntax to convert to</param>
        [Theory]
        [MemberData(nameof(Get8BitTranscoderCombos))]
        public async Task Gen_GivenSupportedTransferSyntax_WhenTranscodingRealUSFile_OutputCorrectlySaved(
            string tsFrom,
            string tsTo)
        {
            var fromTransferSyntax = (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(tsFrom).GetValue(null);
            var toTransferSyntax = (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(tsTo).GetValue(null);

            output.WriteLine(
                $"Converting from {fromTransferSyntax}({fromTransferSyntax.UID.UID}) to {toTransferSyntax}({toTransferSyntax.UID.UID})");

            var dirName = "realUSTranscoded";
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            var dicomFiles = Samples.GetDicomFilesForTranscoding();
            var dicomFile = dicomFiles.FirstOrDefault(f => (Path.GetFileNameWithoutExtension(f.File.Name) == tsFrom));

            Assert.Equal(dicomFile.Dataset.InternalTransferSyntax, fromTransferSyntax);

            var transcoder = new DicomTranscoder(
                dicomFile.Dataset.InternalTransferSyntax,
                toTransferSyntax);

            dicomFile = transcoder.Transcode(dicomFile);

            await dicomFile.SaveAsync(Path.Combine(dirName, $"{tsFrom}-{tsTo}.dcm"));
        }

        /// <summary>
        /// Due to a bug in 4.0.1 (see isolated test above), this will not generate JPEGP1 ↔ JPEGP2_4 transcodings
        /// Also, transcodings from those JPEG images will be garbled upon visual inspection. This test will
        /// start failing when the issue is fixed (a good thing!)
        /// </summary>
        /// <param name="tsFrom">Transfer syntax to convert from</param>
        /// <param name="tsTo">Transfer syntax to convert to</param>
        [Theory]
        [MemberData(nameof(Get8BitTranscoderCombos))]
        public async Task Gen_GivenSupportedTransferSyntax_WhenTranscoding8bitSynthMonoData_OutputCorrectlySaved(
            string tsFrom,
            string tsTo)
        {
            // These combinations do not work at all for generated 8bit monochrome data
            var brokenTranscodings = new List<(DicomTransferSyntax, DicomTransferSyntax)>()
            {
                (DicomTransferSyntax.JPEGProcess1, DicomTransferSyntax.JPEG2000Lossless),
                (DicomTransferSyntax.JPEGProcess1, DicomTransferSyntax.JPEG2000Lossy),
                (DicomTransferSyntax.JPEGProcess1, DicomTransferSyntax.JPEGProcess1),
                (DicomTransferSyntax.JPEGProcess1, DicomTransferSyntax.JPEGProcess2_4),
                (DicomTransferSyntax.JPEGProcess2_4, DicomTransferSyntax.JPEG2000Lossless),
                (DicomTransferSyntax.JPEGProcess2_4, DicomTransferSyntax.JPEG2000Lossy),
                (DicomTransferSyntax.JPEGProcess2_4, DicomTransferSyntax.JPEGProcess1),
                (DicomTransferSyntax.JPEGProcess2_4, DicomTransferSyntax.JPEGProcess2_4),
            };

            var fromTransferSyntax = (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(tsFrom).GetValue(null);
            var toTransferSyntax = (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(tsTo).GetValue(null);

            output.WriteLine(
                $"Converting from {fromTransferSyntax}({fromTransferSyntax.UID.UID}) to {toTransferSyntax}({toTransferSyntax.UID.UID})");

            var dirName = "genTranscoded8bit";
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            var dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(transferSyntax: fromTransferSyntax.UID.UID);

            Assert.Equal(dicomFile.Dataset.InternalTransferSyntax, fromTransferSyntax);

            var transcoder = new DicomTranscoder(
                dicomFile.Dataset.InternalTransferSyntax,
                toTransferSyntax);

            if (brokenTranscodings.Contains((fromTransferSyntax, toTransferSyntax)))
            {
                Assert.Throws<InvalidOperationException>(() =>
                    dicomFile = transcoder.Transcode(dicomFile));
            }
            else
            {
                dicomFile = transcoder.Transcode(dicomFile);

                await dicomFile.SaveAsync(Path.Combine(dirName, $"{tsFrom}-{tsTo}.dcm"));
            }
        }

        /// <summary>
        /// All transcodings work here, but some J2K-J2K transcodings produce corrupted images. Note that J2K is not officially supported by
        /// fo-dicom 4.0.1
        /// </summary>
        /// <param name="tsFrom">Transfer syntax to convert from</param>
        /// <param name="tsTo">Transfer syntax to convert to</param>
        [Theory]
        [MemberData(nameof(Get16BitTranscoderCombos))]
        public async Task Gen_GivenSupportedTransferSyntax_WhenTranscoding16bitSynthMonoData_OutputCorrectlySaved(
            string tsFrom,
            string tsTo)
        {
            var fromTransferSyntax = (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(tsFrom).GetValue(null);
            var toTransferSyntax = (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(tsTo).GetValue(null);

            output.WriteLine(
                $"Converting from {fromTransferSyntax}({fromTransferSyntax.UID.UID}) to {toTransferSyntax}({toTransferSyntax.UID.UID})");

            var dirName = "genTranscoded16bit";
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            var dicomFile = Samples.CreateRandomDicomFileWith16BitPixelData(transferSyntax: fromTransferSyntax.UID.UID);

            Assert.Equal(dicomFile.Dataset.InternalTransferSyntax, fromTransferSyntax);

            var transcoder = new DicomTranscoder(
                dicomFile.Dataset.InternalTransferSyntax,
                toTransferSyntax);

            dicomFile = transcoder.Transcode(dicomFile);

            await dicomFile.SaveAsync(Path.Combine(dirName, $"{tsFrom}-{tsTo}.dcm"));
        }
    }
}
