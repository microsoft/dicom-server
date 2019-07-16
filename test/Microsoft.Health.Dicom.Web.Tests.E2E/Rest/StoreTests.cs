// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Resources.Store;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class StoreTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        public StoreTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            Client = new DicomWebClient(fixture.HttpClient);
        }

        protected DicomWebClient Client { get; set; }

        [Fact]
        public async Task GivenRandomContent_WhenStoring_TheServerShouldReturnConflict()
        {
            using (var stream = new MemoryStream())
            {
                HttpResult<DicomDataset> response = await Client.PostAsync(new[] { stream });
                Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            }
        }

        [Fact]
        public async Task GivenARequestWithInvalidStudyInstanceUID_WhenStoring_TheServerShouldReturnBadRequest()
        {
            using (var stream = new MemoryStream())
            {
                HttpResult<DicomDataset> response = await Client.PostAsync(new[] { stream }, studyInstanceUID: new string('b', 65));
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Theory]
        [InlineData("application/data")]
        [InlineData("application/dicom")]
        public async void GivenAnIncorrectAcceptHeader_WhenStoring_TheServerShouldReturnNotAcceptable(string acceptHeader)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, acceptHeader);

            using (HttpResponseMessage response = await Client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
            }
        }

        [Fact]
        public async void GivenAnNonMultipartRequest_WhenStoring_TheServerShouldReturnUnsupportedMediaType()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);
            request.Content = new ByteArrayContent(new byte[] { 1, 2, 3 });

            using (HttpResponseMessage response = await Client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }

        [Fact]
        public async void GivenAllDifferentStudyInstanceUIDs_WhenStoringWithProvidedStudyInstanceUID_TheServerShouldReturnConflict()
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile();
            DicomFile dicomFile2 = Samples.CreateRandomDicomFile();

            var studyInstanceUID = Guid.NewGuid().ToString();
            HttpResult<DicomDataset> response = await Client.PostAsync(
                new[] { dicomFile1, dicomFile2 }, studyInstanceUID);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.NotNull(response.Value);
            Assert.True(response.Value.Count() == 2);

            Assert.EndsWith($"studies/{studyInstanceUID}", response.Value.GetSingleValue<string>(DicomTag.RetrieveURL));

            DicomSequence failures = response.Value.GetSequence(DicomTag.FailedSOPSequence);
            Assert.True(failures.Count() == 2);

            foreach (DicomDataset failedDataset in failures)
            {
                Assert.True(failedDataset.Count() == 3);
                Assert.Equal(StoreTransactionResponseBuilder.MismatchStudyInstanceUID, failedDataset.GetSingleValue<ushort>(DicomTag.FailureReason));

                var referencedSopClassUID = failedDataset.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID);
                var referencedSopInstanceUID = failedDataset.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID);
                Assert.True(
                    dicomFile1.Dataset.GetSingleValue<string>(DicomTag.SOPClassUID) == referencedSopClassUID ||
                    dicomFile2.Dataset.GetSingleValue<string>(DicomTag.SOPClassUID) == referencedSopClassUID);
                Assert.True(
                    dicomFile1.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID) == referencedSopInstanceUID ||
                    dicomFile2.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID) == referencedSopInstanceUID);
            }
        }
    }
}
