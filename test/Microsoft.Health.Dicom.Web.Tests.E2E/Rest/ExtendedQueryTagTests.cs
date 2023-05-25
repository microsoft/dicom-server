// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Microsoft.Health.Operations;
using Xunit;
using FunctionsStartup = Microsoft.Health.Dicom.Functions.App.Startup;
using WebStartup = Microsoft.Health.Dicom.Web.Startup;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public class ExtendedQueryTagTests : IClassFixture<WebJobsIntegrationTestFixture<WebStartup, FunctionsStartup>>, IAsyncLifetime
{
    private const string ErroneousDicomAttributesHeader = "erroneous-dicom-attributes";
    private readonly IDicomWebClient _client;
    private readonly DicomTagsManager _tagManager;
    private readonly DicomInstancesManager _instanceManager;

    // Note: Different tags should be used for BVTs so they can be run concurrently without issues

    public ExtendedQueryTagTests(WebJobsIntegrationTestFixture<WebStartup, FunctionsStartup> fixture)
    {
        _client = EnsureArg.IsNotNull(fixture, nameof(fixture)).GetDicomWebClient();
        _tagManager = new DicomTagsManager(_client);
        _instanceManager = new DicomInstancesManager(_client);
    }

    [Fact]
    [Trait("Category", "bvt")]
    public async Task GivenExtendedQueryTag_WhenReindexing_ThenShouldSucceed()
    {
        DicomTag genderTag = DicomTag.PatientSex;
        DicomTag filmTag = DicomTag.NumberOfFilms;

        // Try to delete these extended query tags.
        await _tagManager.DeleteExtendedQueryTagAsync(genderTag.GetPath());
        await _tagManager.DeleteExtendedQueryTagAsync(filmTag.GetPath());

        // Define DICOM files
        DicomDataset instance1 = Samples.CreateRandomInstanceDataset();
        instance1.Add(genderTag, "M");
        instance1.Add(filmTag, "12");

        DicomDataset instance2 = Samples.CreateRandomInstanceDataset();
        instance2.Add(genderTag, "O");
        instance2.Add(filmTag, "03");

        // Upload files
        Assert.True((await _instanceManager.StoreAsync(new DicomFile(instance1))).IsSuccessStatusCode);
        Assert.True((await _instanceManager.StoreAsync(new DicomFile(instance2))).IsSuccessStatusCode);

        // Add extended query tag
#pragma warning disable CS0618
        Assert.Equal(
            OperationStatus.Completed,
            await _tagManager.AddTagsAsync(
                new AddExtendedQueryTagEntry { Path = genderTag.GetPath(), VR = genderTag.GetDefaultVR().Code, Level = QueryTagLevel.Study },
                new AddExtendedQueryTagEntry { Path = filmTag.GetPath(), VR = filmTag.GetDefaultVR().Code, Level = QueryTagLevel.Study }));
#pragma warning restore CS0618

        // Check specific tag
        DicomWebResponse<GetExtendedQueryTagEntry> getResponse;
        GetExtendedQueryTagEntry entry;

        getResponse = await _client.GetExtendedQueryTagAsync(genderTag.GetPath());
        entry = await getResponse.GetValueAsync();
        Assert.Null(entry.Errors);
        Assert.Equal(QueryStatus.Enabled, entry.QueryStatus);

        getResponse = await _client.GetExtendedQueryTagAsync(filmTag.GetPath());
        entry = await getResponse.GetValueAsync();
        Assert.Null(entry.Errors);
        Assert.Equal(QueryStatus.Enabled, entry.QueryStatus);

        // Query multiple tags
        // Note: We don't necessarily need to check the tags are the above ones, as another test may have added ones beforehand
        var multipleTags = await _tagManager.GetTagsAsync(2, 0);
        Assert.Equal(2, multipleTags.Count);

        Assert.Equal(multipleTags[0].Path, (await _tagManager.GetTagsAsync(1, 0)).Single().Path);
        Assert.Equal(multipleTags[1].Path, (await _tagManager.GetTagsAsync(1, 1)).Single().Path);

        // QIDO
        DicomWebAsyncEnumerableResponse<DicomDataset> queryResponse = await _client.QueryInstancesAsync($"{filmTag.GetPath()}=0003");
        DicomDataset[] instances = await queryResponse.ToArrayAsync();
        Assert.Contains(instances, instance => instance.ToInstanceIdentifier().Equals(instance2.ToInstanceIdentifier()));
    }

    [Fact]
    [Trait("Category", "bvt")]
    public async Task GivenExtendedQueryTagUsingPrivateTags_WhenReindexing_ThenShouldSucceed()
    {
        DicomTag privateTag = new DicomTag(0x00f1, 0x0010, "MY_PRIVATE_CREATOR");
        DicomTag filmTag = DicomTag.NumberOfFilms;

        // Try to delete these extended query tags.
        await _tagManager.DeleteExtendedQueryTagAsync(privateTag.GetPath());
        await _tagManager.DeleteExtendedQueryTagAsync(filmTag.GetPath());

        // Define DICOM files
        DicomDataset instance1 = Samples.CreateRandomInstanceDataset();
        instance1.Add(privateTag, "M");
        instance1.Add(filmTag, "12");

        DicomDataset instance2 = Samples.CreateRandomInstanceDataset();
        instance2.Add(privateTag, "O");
        instance2.Add(filmTag, "03");

        // Upload files
        Assert.True((await _instanceManager.StoreAsync(new DicomFile(instance1))).IsSuccessStatusCode);
        Assert.True((await _instanceManager.StoreAsync(new DicomFile(instance2))).IsSuccessStatusCode);

        // Add extended query tag
#pragma warning disable CS0618
        Assert.Equal(
            OperationStatus.Completed,
            await _tagManager.AddTagsAsync(
                new AddExtendedQueryTagEntry { Path = privateTag.GetPath(), VR = privateTag.GetDefaultVR().Code, Level = QueryTagLevel.Study },
                new AddExtendedQueryTagEntry { Path = filmTag.GetPath(), VR = filmTag.GetDefaultVR().Code, Level = QueryTagLevel.Study }));
#pragma warning restore CS0618

        // Check specific tag
        DicomWebResponse<GetExtendedQueryTagEntry> getResponse;
        GetExtendedQueryTagEntry entry;

        getResponse = await _client.GetExtendedQueryTagAsync(privateTag.GetPath());
        entry = await getResponse.GetValueAsync();
        Assert.Null(entry.Errors);
        Assert.Equal(QueryStatus.Enabled, entry.QueryStatus);

        getResponse = await _client.GetExtendedQueryTagAsync(filmTag.GetPath());
        entry = await getResponse.GetValueAsync();
        Assert.Null(entry.Errors);
        Assert.Equal(QueryStatus.Enabled, entry.QueryStatus);

        // Query multiple tags
        // Note: We don't necessarily need to check the tags are the above ones, as another test may have added ones beforehand
        var multipleTags = await _tagManager.GetTagsAsync(2, 0);
        Assert.Equal(2, multipleTags.Count);

        Assert.Equal(multipleTags[0].Path, (await _tagManager.GetTagsAsync(1, 0)).Single().Path);
        Assert.Equal(multipleTags[1].Path, (await _tagManager.GetTagsAsync(1, 1)).Single().Path);

        // QIDO
        DicomWebAsyncEnumerableResponse<DicomDataset> queryResponse = await _client.QueryInstancesAsync($"{filmTag.GetPath()}=0003");
        DicomDataset[] instances = await queryResponse.ToArrayAsync();
        Assert.Contains(instances, instance => instance.ToInstanceIdentifier().Equals(instance2.ToInstanceIdentifier()));
    }

    [Fact]
    [Trait("Category", "bvt")]
    public async Task GivenExtendedQueryTagWithErrors_WhenReindexing_ThenShouldSucceedWithErrors()
    {
        // Define tags
        DicomTag tag = DicomTag.PatientAge;
        string tagValue = "053Y";

        // Try to delete this extended query tag if it exists.
        await _tagManager.DeleteExtendedQueryTagAsync(tag.GetPath());

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
#pragma warning disable CS0618
        Assert.Equal(
            OperationStatus.Completed,
            await _tagManager.AddTagsAsync(new AddExtendedQueryTagEntry { Path = tag.GetPath(), VR = tag.GetDefaultVR().Code, Level = QueryTagLevel.Instance }));
#pragma warning restore CS0618

        // Check specific tag
        GetExtendedQueryTagEntry actual = await _tagManager.GetTagAsync(tag.GetPath());
        Assert.Equal(tag.GetPath(), actual.Path);
        Assert.True(actual.Errors.Count >= 2, "Expected at least 2 errors.");
        Assert.Equal(QueryStatus.Disabled, actual.QueryStatus); // It should be disabled by default

        // Verify Errors
        // Note: Use pageSize = 1 to test pagination
        List<ExtendedQueryTagError> errors = await _tagManager.GetTagErrorsAsync(tag.GetPath(), 1).ToListAsync();

        Assert.Equal(actual.Errors.Count, errors.Count);
        Assert.Contains(errors[0].ErrorMessage, errors.Select(x => x.ErrorMessage));
        Assert.Contains(errors[1].ErrorMessage, errors.Select(x => x.ErrorMessage));

        // Check that the reference API returns the same values
        // Note: The reference only returns the first default page of results
        var errorsByReference = await _client.ResolveReferenceAsync(actual.Errors);
        Assert.Equal((await _tagManager.GetTagErrorsAsync(tag.GetPath(), 100, 0)).Select(x => x.ErrorMessage), (await errorsByReference.GetValueAsync()).Select(x => x.ErrorMessage));

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
        Assert.Contains(string.Format(CultureInfo.CurrentCulture, "The field '[0].{0}' in request body is invalid: The Dicom Tag Property {0} must be specified and must not be null, empty or whitespace", missingProperty), response.Content.ReadAsStringAsync().Result);
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
        Assert.Equal("The field '$[0].Level' in request body is invalid: Expected value 'Studys' to be one of the following values: ['Instance', 'Series', 'Study']", response.Content.ReadAsStringAsync().Result);
    }

    [Fact]
    public async Task GivenInvalidDSIS_WhenStoring_ThenServerShouldReturnOK()
    {
        DicomTag dsTag = DicomTag.PatientSize;
        DicomTag isTag = DicomTag.ReferencedFrameNumber;
        await _tagManager.DeleteExtendedQueryTagAsync(dsTag.GetPath());
        await _tagManager.DeleteExtendedQueryTagAsync(isTag.GetPath());
        DicomFile dicomFile = Samples.CreateRandomDicomFile();
        var dataSet = dicomFile.Dataset.NotValidated();
        dataSet.Add(dsTag, "InvalidDSValue");
        dataSet.Add(isTag, "InvalidISValue");
        using DicomWebResponse<DicomDataset> response = await _instanceManager.StoreAsync(dicomFile);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GivenInvalidISAndIndexed_WhenStoring_ThenServerShouldReturnConflict()
    {
        DicomTag tag = DicomTag.StageNumber;

        await _tagManager.DeleteExtendedQueryTagAsync(tag.GetPath());

        // add extended query tag
#pragma warning disable CS0618
        Assert.Equal(
            OperationStatus.Completed,
            await _tagManager.AddTagsAsync(new AddExtendedQueryTagEntry { Level = QueryTagLevel.Instance, Path = tag.GetPath() }));
#pragma warning restore CS0618

        // validate
        DicomFile dicomFile = Samples.CreateRandomDicomFile();
        var dataSet = dicomFile.Dataset.NotValidated();
        dataSet.Add(tag, "InvalidISValue");
        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(dicomFile));
        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
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
        => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _tagManager.DisposeAsync();
        await _instanceManager.DisposeAsync();
    }
}
