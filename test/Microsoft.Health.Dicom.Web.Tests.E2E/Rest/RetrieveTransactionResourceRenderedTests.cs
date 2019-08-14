// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Microsoft.Health.Dicom.Core.Features.Resources.Retrieve.BitmapRendering;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class RetrieveTransactionResourceRenderedTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly ITestOutputHelper output;

        public RetrieveTransactionResourceRenderedTests(HttpIntegrationTestFixture<Startup> fixture, ITestOutputHelper output)
        {
            Client = new DicomWebClient(fixture.HttpClient);
            this.output = output;
        }

        protected DicomWebClient Client { get; set; }

        [Fact]
        public void Gen_GivenValid8BitSampleData_WhenRendering_ShoudlSaveProperly()
        {
            var dirName = "genRendered8bit";
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

                    using (var bmp = new DicomImage(dicomFile.Dataset).RenderImage().AsClonedBitmap())
                    {
                        bmp.Save(Path.Combine(dirName, $"{ts.name}.png"), ImageFormat.Png);

                        var resizedSize = (100, 100);
                        var bmpResized = new Bitmap(resizedSize.Item1, resizedSize.Item2);
                        using (var graphics = Graphics.FromImage(bmpResized))
                        {
                            graphics.CompositingQuality = CompositingQuality.HighSpeed;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.DrawImage(bmp, 0, 0, resizedSize.Item1, resizedSize.Item2);
                            bmpResized.Save(Path.Combine(dirName, $"{ts.name}_thumb.png"), ImageFormat.Png);
                        }
                    }

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
        public void ConvertCanHandleParallelProcessing()
        {
            var maxFiles = 500;
            var random = new Random();

            var fromList = new List<string>
            {
                "DeflatedExplicitVRLittleEndian", "ExplicitVRBigEndian", "ExplicitVRLittleEndian", "ImplicitVRLittleEndian",
                "JPEG2000Lossless", "JPEG2000Lossy", "RLELossless",
            };

            var fromTsList = fromList.Select(x =>
                (name: x, transferSyntax: (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(x).GetValue(null)));

            var files = new List<DicomFile>();

            for (int i = 0; i < maxFiles; i++)
            {
                files.Add(Samples.CreateRandomDicomFileWith8BitPixelData(transferSyntax: fromTsList.ElementAt(random.Next(fromList.Count - 1)).transferSyntax.UID.UID));
            }

            Parallel.ForEach(
                files,
                file =>
                {
                    var bmp = new DicomImage(file.Dataset).RenderImage().AsClonedBitmap();

                    var ms = new MemoryStream();

                    bmp.Save(ms, ImageFormat.Png);
                    Assert.NotEqual(0, ms.Length);
                });
        }

        [Fact]
        public void Gen_GivenValid16BitSampleData_WhenRendering_ShoudlSaveProperly()
        {
            var dirName = "genRendered16bit";
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

                    var image = new DicomImage(dicomFile.Dataset).RenderImage();

                    var ici = ImageCodecInfo.GetImageEncoders().FirstOrDefault(x => x.MimeType == "image/jpeg");
                    EncoderParameters ep =
                        new EncoderParameters(1) { Param = { [0] = new EncoderParameter(Encoder.Quality, 1L) } };

                    // image.AsClonedBitmap().Save(Path.Combine(dirName, $"{ts.name}.png"), ImageFormat.Png);

                    image.AsClonedBitmap().Save(Path.Combine(dirName, $"{ts.name}.jpg"), ici, null);

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
        public async Task GivenValidDicomFile_WhenRetrievingRendered_ShouldReturnValidImage()
        {
            var fromList = new List<string>
            {
                "DeflatedExplicitVRLittleEndian", "ExplicitVRBigEndian", "ExplicitVRLittleEndian", "ImplicitVRLittleEndian",
                "JPEG2000Lossless", "JPEG2000Lossy", "RLELossless",

                // "JPEGProcess1", "JPEGProcess2_4", <-- are not supported for 16bit data
            };
            var fromTsList = fromList.Select(x =>
                (name: x, transferSyntax: (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(x).GetValue(null)));

            foreach (var ts in fromTsList)
            {
                var dicomFile = Samples.CreateRandomDicomFileWith16BitPixelData(transferSyntax: ts.transferSyntax.UID.UID);

                HttpResult<DicomDataset> postResponse = await Client.PostAsync(new[] { dicomFile });
                Assert.True(postResponse.StatusCode == HttpStatusCode.OK);

                var studyInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
                var seriesInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
                var sopInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

                var getResponse = await Client.GetInstanceRenderedAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, "image/jpeg", false);
                Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

                var img = Image.FromStream(getResponse.Value.Single());
                Assert.Equal(ImageFormat.Jpeg, img.RawFormat);

                // TODO: get frames rendered
                // TODO: check proper return content-type

            }
        }
    }
}
