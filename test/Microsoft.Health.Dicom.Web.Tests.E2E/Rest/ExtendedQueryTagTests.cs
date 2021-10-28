// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class ExtendedQueryTagTests : IClassFixture<HttpIntegrationTestFixture<Startup>>, IAsyncLifetime
    {
        private const string ErroneousDicomAttributesHeader = "erroneous-dicom-attributes";
        private readonly IDicomWebClient _client;
        private readonly DicomTagsManager _tagManager;
        private readonly DicomInstancesManager _instanceManager;
        private readonly bool _isUsingInProcTestServer;

        public ExtendedQueryTagTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            EnsureArg.IsNotNull(fixture.Client, nameof(fixture.Client));
            _client = fixture.Client;
            _isUsingInProcTestServer = fixture.IsUsingInProcTestServer;
            _tagManager = new DicomTagsManager(_client);
            _instanceManager = new DicomInstancesManager(_client);
        }

        [Fact]
        public async Task GivenExtendedQueryTag_WhenReindexing_ThenShouldSucceed()
        {
            if (_isUsingInProcTestServer)
            {
                // AzureFunction doesn't have InProc test sever, skip this test.
                return;
            }

            string weightPrivateCreator = "PatientWeight";
            DicomTag weightTag = RandomDicomTag(isPrivate: true, privateCreator: weightPrivateCreator);
            string weightTagVRCode = DicomVR.DS.Code;

            // Try to delete weight private extended query tag if it exists.
            await DeleteExtendedQueryTagIfExists(weightTag);

            string sizePrivateCreator = "PatientSize";
            DicomTag sizeTag = RandomDicomTag(isPrivate: true, privateCreator: sizePrivateCreator);
            string sizeTagVRCode = DicomVR.DS.Code;

            // Try to delete size private extended query tag if it exists.
            await DeleteExtendedQueryTagIfExists(sizeTag);

            // Define DICOM files
            DicomDataset instance1 = Samples.CreateRandomInstanceDataset();
            instance1.Add(weightTag, 68.0M);
            instance1.Add(sizeTag, 1.78M);

            DicomDataset instance2 = Samples.CreateRandomInstanceDataset();
            instance2.Add(weightTag, 50.0M);
            instance2.Add(sizeTag, 1.5M);

            // Upload files
            await _instanceManager.StoreAsync(new DicomFile(instance1));
            await _instanceManager.StoreAsync(new DicomFile(instance2));

            // Add extended query tag
            OperationStatus operation = await _tagManager.AddTagsAsync(
                new AddExtendedQueryTagEntry[]
                {
                    new AddExtendedQueryTagEntry { Path = weightTag.GetPath(), VR = weightTagVRCode, Level = QueryTagLevel.Study },
                    new AddExtendedQueryTagEntry { Path = sizeTag.GetPath(), VR = sizeTagVRCode, Level = QueryTagLevel.Study },
                });
            Assert.Equal(OperationRuntimeStatus.Completed, operation.Status);

            // Check specific tag
            DicomWebResponse<GetExtendedQueryTagEntry> getResponse;

            getResponse = await _client.GetExtendedQueryTagAsync(weightTag.GetPath());
            Assert.Null((await getResponse.GetValueAsync()).Errors);

            getResponse = await _client.GetExtendedQueryTagAsync(sizeTag.GetPath());
            Assert.Null((await getResponse.GetValueAsync()).Errors);

            // Query multiple tags
            // Note: We don't necessarily need to check the tags are the above ones, as another test may have added ones beforehand
            var multipleTags = await _tagManager.GetTagsAsync(2, 0);
            Assert.Equal(2, multipleTags.Count);

            Assert.Equal(multipleTags[0].Path, (await _tagManager.GetTagsAsync(1, 0)).Single().Path);
            Assert.Equal(multipleTags[1].Path, (await _tagManager.GetTagsAsync(1, 1)).Single().Path);

            // QIDO
            DicomWebAsyncEnumerableResponse<DicomDataset> queryResponse = await _client.QueryInstancesAsync($"{weightTag.GetPath()}={50}");
            DicomDataset[] instances = await queryResponse.ToArrayAsync();
            Assert.Contains(instances, instance => instance.ToInstanceIdentifier().Equals(instance2.ToInstanceIdentifier()));
        }

        [Fact]
        public async Task GivenExtendedQueryTagWithErrors_WhenReindexing_ThenShouldSucceedWithErrors()
        {
            if (_isUsingInProcTestServer)
            {
                // AzureFunction doesn't have InProc test sever, skip this test.
                return;
            }

            // Use private tag
            string privateCreator = "PrivateCreator1";
            DicomTag tag = RandomDicomTag(isPrivate: true, privateCreator: privateCreator);
            string tagValue = "053Y";
            string tagVRCode = DicomVR.AS.Code;

            // Try to delete this extended query tag if it exists.
            await DeleteExtendedQueryTagIfExists(tag);

            // Define DICOM files
            DicomDataset instance1 = Samples.CreateRandomInstanceDataset();
            DicomDataset instance2 = Samples.CreateRandomInstanceDataset();
            DicomDataset instance3 = Samples.CreateRandomInstanceDataset();

            // Annotate files
            // (Disable Auto-validate)
            instance1.NotValidated();
            instance2.NotValidated();

            instance1.Add(tag, "foobar");
            instance2.Add(tag, "invalid");
            instance3.Add(tag, tagValue);

            // Upload files (with a few errors)
            await _instanceManager.StoreAsync(new DicomFile(instance1));
            await _instanceManager.StoreAsync(new DicomFile(instance2));
            await _instanceManager.StoreAsync(new DicomFile(instance3));

            // Add extended query tags
            var operationStatus = await _tagManager.AddTagsAsync(
                new AddExtendedQueryTagEntry[]
                {
                    new AddExtendedQueryTagEntry { Path = tag.GetPath(), VR = tagVRCode, PrivateCreator = privateCreator, Level = QueryTagLevel.Instance },
                });
            Assert.Equal(OperationRuntimeStatus.Completed, operationStatus.Status);

            // Check specific tag
            GetExtendedQueryTagEntry actual = await _tagManager.GetTagAsync(tag.GetPath());
            Assert.Equal(tag.GetPath(), actual.Path);
            Assert.Equal(2, actual.Errors.Count);
            // It should be disabled by default
            Assert.Equal(QueryStatus.Disabled, actual.QueryStatus);

            // Verify Errors
            var errors = await _tagManager.GetTagErrorsAsync(tag.GetPath(), 2, 0);
            Assert.Equal(2, errors.Count);

            Assert.Equal(errors[0].ErrorMessage, (await _tagManager.GetTagErrorsAsync(tag.GetPath(), 1, 0)).Single().ErrorMessage);
            Assert.Equal(errors[1].ErrorMessage, (await _tagManager.GetTagErrorsAsync(tag.GetPath(), 1, 1)).Single().ErrorMessage);

            var exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.QueryInstancesAsync($"{tag.GetPath()}={tagValue}"));
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);

            // Enable QIDO on Tag
            actual = await _tagManager.UpdateExtendedQueryTagAsync(tag.GetPath(), new UpdateExtendedQueryTagEntry() { QueryStatus = QueryStatus.Enabled });
            Assert.Equal(QueryStatus.Enabled, actual.QueryStatus);

            var response = await _client.QueryInstancesAsync($"{tag.GetPath()}={tagValue}");

            Assert.True(response.ResponseHeaders.Contains(ErroneousDicomAttributesHeader));
            var values = response.ResponseHeaders.GetValues(ErroneousDicomAttributesHeader);
            Assert.Single(values);
            Assert.Equal(tag.GetPath(), values.First());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify result
            DicomDataset[] instances = await response.ToArrayAsync();
            Assert.Contains(instances, instance => instance.ToInstanceIdentifier().Equals(instance3.ToInstanceIdentifier()));
        }

        [Theory]
        [MemberData(nameof(GetRequestBodyWithMissingProperty))]
        public async Task GivenMissingPropertyInRequestBody_WhenCallingPostAsync_ThenShouldThrowException(string requestBody, string missingProperty)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{DicomApiVersions.Latest}/extendedquerytags");
            {
                request.Content = new StringContent(requestBody);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType);
            }

            HttpResponseMessage response = await _client.HttpClient.SendAsync(request, default(CancellationToken))
                .ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(string.Format("The field '[0].{0}' in request body is invalid: The Dicom Tag Property {0} must be specified and must not be null, empty or whitespace", missingProperty), response.Content.ReadAsStringAsync().Result);
        }

        [Fact]
        public async Task GivenInvalidTagLevelInRequestBody_WhenCallingPostAync_ThenShouldThrowException()
        {
            string requestBody = "[{\"Path\":\"00100040\",\"Level\":\"Studys\"}]";
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{DicomApiVersions.Latest}/extendedquerytags");
            {
                request.Content = new StringContent(requestBody);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType);
            }

            HttpResponseMessage response = await _client.HttpClient.SendAsync(request, default(CancellationToken)).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("The field '[0].Level' in request body is invalid: Input Dicom Tag Level 'Studys' is invalid. It must have value 'Study', 'Series' or 'Instance'.", response.Content.ReadAsStringAsync().Result);
        }

        private async Task DeleteExtendedQueryTagIfExists(DicomTag tag)
        {
            // Try to delete this extended query tag if it exists.
            try
            {
                await _tagManager.DeleteExtendedQueryTagAsync(string.Concat(tag.Group, tag.Element));
            }
            catch (Exception ex) when (ex.Message.StartsWith("NotFound"))
            {
                // If extended query tag does not exist, it cannot be deleted.
            }
        }

        private static DicomTag RandomDicomTag(bool isPrivate = false, string privateCreator = null)
        {
            var random = new Random();

            // Private groups should end with an odd number
            int randomGroup = random.Next(0, 9999);
            ushort group = isPrivate ?
                randomGroup % 2 == 1 ? Convert.ToUInt16(randomGroup) : Convert.ToUInt16(randomGroup + 1) :
                Convert.ToUInt16(randomGroup);

            ushort element = Convert.ToUInt16(random.Next(0, 9999));

            return string.IsNullOrWhiteSpace(privateCreator) ? new DicomTag(group, element) : new DicomTag(group, element, privateCreator);
        }

        public static IEnumerable<object[]> GetRequestBodyWithMissingProperty
        {
            get
            {
                yield return new object[] { "[{\"Path\":\"00100040\"}]", "Level" };
                yield return new object[] { "[{\"Path\":\"\",\"Level\":\"Study\"}]", "Path" };
            }
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _tagManager.DisposeAsync();
            await _instanceManager.DisposeAsync();
        }
    }
}
