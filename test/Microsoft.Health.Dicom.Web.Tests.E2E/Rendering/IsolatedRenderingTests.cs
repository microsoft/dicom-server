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
using Microsoft.Health.Dicom.Core.Features.Resources.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Resources.Retrieve.BitmapRendering;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Rest;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rendering
{
    public class IsolatedRenderingTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly ITestOutputHelper output;

        public IsolatedRenderingTests(HttpIntegrationTestFixture<Startup> fixture, ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData("image/jpeg", "jpg", "Jpeg")]
        [InlineData("image/png", "png", "Png")]
        public void Gen_GivenValidSampleData_WhenRendering_ShouldSaveProperly(string mimeType, string fileExtension, string format)
        {
            var imageFormat = (ImageFormat)typeof(ImageFormat).GetProperty(format).GetValue(null);
            var dirName = "genRendered8bit";
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

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

            var dicomList = Samples.GetDicomFilesForTranscoding().Where(f => (Path.GetFileNameWithoutExtension(f.File.Name) == "ExplicitVRLittleEndian"));

            fromTsList = fromTsList.Concat(dicomList.Select(x => (name: Path.GetFileName(x.File.Name), bits: "n", dicomFile: x)));

            var filesGenerated = 0;

            foreach (var ts in fromTsList)
            {
                try
                {
                    var ms = new DicomImage(ts.dicomFile.Dataset).ToRenderedMemoryStream(ImageRepresentationModel.Parse(mimeType));
                    var img = Image.FromStream(ms);
                    Assert.Equal(imageFormat, img.RawFormat);

                    img.Save(Path.Combine(dirName, $"{ts.name}_{ts.bits}.{fileExtension}"));

                    ms = new DicomImage(ts.dicomFile.Dataset).ToRenderedMemoryStream(ImageRepresentationModel.Parse(mimeType), thumbnail: true);
                    img = Image.FromStream(ms);
                    Assert.Equal(imageFormat, img.RawFormat);
                    Assert.Equal(100, img.Width);
                    Assert.Equal(100, img.Height);

                    img.Save(Path.Combine(dirName, $"{ts.name}_{ts.bits}_thumb.{fileExtension}"));

                    filesGenerated++;
                }
                catch
                {
                    output.WriteLine($"Failed to render {ts.bits}bit {ts.name}");
                }
            }

            Assert.Equal(fromTsList.Count(), filesGenerated);
        }
    }
}
