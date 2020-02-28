// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Dicom;
using Dicom.Imaging;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Resources.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Resources.Retrieve
{
    public class RetrieveDicomResourceHandlerTests
    {
        private readonly ITestOutputHelper _output;
        private readonly RetrieveDicomResourceHandler _retrieveDicomResourceHandler;

        public RetrieveDicomResourceHandlerTests(ITestOutputHelper output)
        {
            var dicomMetadataStore = Substitute.For<IDicomMetadataStore>();
            var dicomDataStore = Substitute.For<IDicomDataStore>();
            var recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

            _retrieveDicomResourceHandler = new RetrieveDicomResourceHandler(dicomMetadataStore, dicomDataStore, recyclableMemoryStreamManager);
            _output = output;
        }

        [Theory]
        [InlineData("image/jpeg", "Jpeg")]
        [InlineData("image/png", "Png")]
        public void Gen_GivenValidSampleData_WhenRendering_ShouldSaveProperly(string mimeType, string format)
        {
            var imageFormat = (ImageFormat)typeof(ImageFormat).GetProperty(format).GetValue(null);

            var fromList8 = new List<string>
            {
                "JPEGProcess1", "JPEGProcess2_4", "DeflatedExplicitVRLittleEndian", "ExplicitVRBigEndian", "ExplicitVRLittleEndian", "ImplicitVRLittleEndian",
                "JPEG2000Lossless", "JPEG2000Lossy", "RLELossless",
            };

            var fromList16 = new List<string>
            {
                "DeflatedExplicitVRLittleEndian", "ExplicitVRBigEndian", "ExplicitVRLittleEndian", "ImplicitVRLittleEndian",
                "JPEG2000Lossless", "JPEG2000Lossy", "RLELossless",

                // "JPEGProcess1", "JPEGProcess2_4", <-- Not supported for 16bit data
            };

            // Generate 8bit images with the appropriate transfer syntax
            var fromTsList = fromList8.Select(x =>
            {
                var ts = (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(x).GetValue(null);
                var f = Samples.CreateRandomDicomFileWith8BitPixelData(transferSyntax: ts.UID.UID);

                return (
                    name: x,
                    bits: "8",
                    dicomFile: f);
            });

            // Generate 16bit images with the appropriate transfer syntax
            fromTsList = fromTsList.Concat(fromList16.Select(x =>
            {
                var ts = (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(x).GetValue(null);
                var f = Samples.CreateRandomDicomFileWith16BitPixelData(transferSyntax: ts.UID.UID);

                return (
                    name: x,
                    bits: "16",
                    dicomFile: f);
            }));

            // Add a real RGB DICOM file from samples folder
            fromTsList = fromTsList.Concat(Samples.GetDicomFilesForTranscoding().Where(f => (Path.GetFileNameWithoutExtension(f.File.Name) == "ExplicitVRLittleEndian")).Select(x => (name: Path.GetFileName(x.File.Name), bits: "n", dicomFile: x)));

            var filesGenerated = 0;

            foreach (var ts in fromTsList)
            {
                try
                {
                    var ms = _retrieveDicomResourceHandler.ToRenderedMemoryStream(new DicomImage(ts.dicomFile.Dataset), ImageRepresentationModel.Parse(mimeType));
                    var img = Image.FromStream(ms);
                    Assert.Equal(imageFormat, img.RawFormat);

                    // Optional - good for debugging - save to disk
                    // img.Save(Path.Combine(dirName, $"{ts.name}_{ts.bits}.{fileExtension}"));

                    ms = _retrieveDicomResourceHandler.ToRenderedMemoryStream(new DicomImage(ts.dicomFile.Dataset), ImageRepresentationModel.Parse(mimeType), thumbnail: true);
                    img = Image.FromStream(ms);
                    Assert.Equal(imageFormat, img.RawFormat);
                    Assert.Equal(200, img.Width);
                    Assert.Equal(200, img.Height);

                    // img.Save(Path.Combine(dirName, $"{ts.name}_{ts.bits}_thumb.{fileExtension}"));

                    filesGenerated++;
                }
                catch
                {
                    _output.WriteLine($"Failed to render {ts.bits}bit {ts.name}");
                }
            }

            Assert.Equal(fromTsList.Count(), filesGenerated);
        }
    }
}
