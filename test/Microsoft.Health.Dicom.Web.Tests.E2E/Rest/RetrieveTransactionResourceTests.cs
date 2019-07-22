// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class RetrieveTransactionResourceTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        public RetrieveTransactionResourceTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            Client = new DicomWebClient(fixture.HttpClient);
        }

        protected DicomWebClient Client { get; set; }

        [Fact]
        public async Task GivenNonExistentIdentifiers_WhenRetrieving_TheServerReturnsNotFound()
        {
            HttpResult<IReadOnlyList<DicomFile>> response1 = await Client.GetStudyAsync(Guid.NewGuid().ToString());
            Assert.Equal(HttpStatusCode.NotFound, response1.StatusCode);
            HttpResult<IReadOnlyList<DicomFile>> response2 = await Client.GetSeriesAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            Assert.Equal(HttpStatusCode.NotFound, response2.StatusCode);
            HttpResult<IReadOnlyList<DicomFile>> response3 = await Client.GetInstanceAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            Assert.Equal(HttpStatusCode.NotFound, response3.StatusCode);
            HttpResult<Stream> response4 = await Client.GetFramesAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), frames: 1);
            Assert.Equal(HttpStatusCode.NotFound, response4.StatusCode);

            // Create a valid Study/ Series/ Instance with one frame
            var studyInstanceUID = Guid.NewGuid().ToString();
            var seriesInstanceUID = Guid.NewGuid().ToString();
            var sopInstanceUID = Guid.NewGuid().ToString();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUID, seriesInstanceUID, sopInstanceUID);
            HttpResult<DicomDataset> storeResponse = await Client.PostAsync(new[] { dicomFile1 }, studyInstanceUID);
            ValidationHelpers.ValidateSuccessSequence(storeResponse.Value.GetSequence(DicomTag.ReferencedSOPSequence), dicomFile1.Dataset);

            HttpResult<IReadOnlyList<DicomFile>> response5 = await Client.GetSeriesAsync(studyInstanceUID, Guid.NewGuid().ToString());
            Assert.Equal(HttpStatusCode.NotFound, response5.StatusCode);
            HttpResult<IReadOnlyList<DicomFile>> response6 = await Client.GetInstanceAsync(studyInstanceUID, seriesInstanceUID, Guid.NewGuid().ToString());
            Assert.Equal(HttpStatusCode.NotFound, response6.StatusCode);
            HttpResult<Stream> response7 = await Client.GetFramesAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, frames: 1);
            Assert.Equal(HttpStatusCode.NotFound, response7.StatusCode);
            HttpResult<Stream> response8 = await Client.GetFramesAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID, frames: 2);
            Assert.Equal(HttpStatusCode.NotFound, response8.StatusCode);
        }

        [Fact]
        public async Task GivenStoredDicomFileWithNoContent_WhenRetrieved_TheFileIsRetrievedCorrectly()
        {
            var studyInstanceUID = Guid.NewGuid().ToString();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID);
            var dicomInstance = DicomInstance.Create(dicomFile1.Dataset);
            HttpResult<DicomDataset> response = await Client.PostAsync(new[] { dicomFile1 }, studyInstanceUID);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            DicomSequence successSequence = response.Value.GetSequence(DicomTag.ReferencedSOPSequence);
            ValidationHelpers.ValidateSuccessSequence(successSequence, dicomFile1.Dataset);

            string studyRetrieveLocation = response.Value.GetSingleValue<string>(DicomTag.RetrieveURL);
            string instanceRetrieveLocation = successSequence.Items[0].GetSingleValue<string>(DicomTag.RetrieveURL);

            HttpResult<IReadOnlyList<DicomFile>> studyByUrlRetrieve = await Client.GetInstancesAsync(new Uri(studyRetrieveLocation));
            ValidateRetrieveTransaction(studyByUrlRetrieve, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, dicomFile1);
            HttpResult<IReadOnlyList<DicomFile>> instanceByUrlRetrieve = await Client.GetInstancesAsync(new Uri(instanceRetrieveLocation));
            ValidateRetrieveTransaction(instanceByUrlRetrieve, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, dicomFile1);

            HttpResult<IReadOnlyList<DicomFile>> studyRetrieve = await Client.GetStudyAsync(dicomInstance.StudyInstanceUID);
            ValidateRetrieveTransaction(studyRetrieve, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, dicomFile1);
            HttpResult<IReadOnlyList<DicomFile>> seriesRetrieve = await Client.GetSeriesAsync(dicomInstance.StudyInstanceUID, dicomInstance.SeriesInstanceUID);
            ValidateRetrieveTransaction(seriesRetrieve, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, dicomFile1);
            HttpResult<IReadOnlyList<DicomFile>> instanceRetrieve = await Client.GetInstanceAsync(dicomInstance.StudyInstanceUID, dicomInstance.SeriesInstanceUID, dicomInstance.SopInstanceUID);
            ValidateRetrieveTransaction(instanceRetrieve, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, dicomFile1);
        }

        private void ValidateRetrieveTransaction(
            HttpResult<IReadOnlyList<DicomFile>> response,
            HttpStatusCode expectedStatusCode,
            DicomTransferSyntax expectedTransferSyntax,
            params DicomFile[] expectedFiles)
        {
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedFiles.Length, response.Value.Count);

            for (var i = 0; i < expectedFiles.Length; i++)
            {
                DicomFile expectedFile = expectedFiles[i];
                var expectedInstance = DicomInstance.Create(expectedFile.Dataset);
                DicomFile actualFile = response.Value.First(x => DicomInstance.Create(x.Dataset).Equals(expectedInstance));

                Assert.Equal(expectedTransferSyntax, response.Value[i].Dataset.InternalTransferSyntax);

                // If the same transfer syntax as original, the files should be exactly the same
                if (expectedFile.Dataset.InternalTransferSyntax == actualFile.Dataset.InternalTransferSyntax)
                {
                    var expectedFileArray = DicomFileToByteArray(expectedFile);
                    var actualFileArray = DicomFileToByteArray(actualFile);

                    Assert.Equal(expectedFileArray.Length, actualFileArray.Length);

                    for (var ii = 0; ii < expectedFileArray.Length; ii++)
                    {
                        Assert.Equal(expectedFileArray[ii], actualFileArray[ii]);
                    }
                }
            }
        }

        private static byte[] DicomFileToByteArray(DicomFile dicomFile)
        {
            using (var memoryStream = new MemoryStream())
            {
                dicomFile.Save(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
