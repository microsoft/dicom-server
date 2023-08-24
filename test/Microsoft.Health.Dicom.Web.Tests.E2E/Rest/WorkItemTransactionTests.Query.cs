// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public partial class WorkItemTransactionTests
{
    [Fact]
    public async Task GivenSearchRequest_WithUnsupportedTag_ReturnBadRequest()
    {
        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
            () => _client.QueryWorkitemAsync("Modality=CT"));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(exception.ResponseMessage, string.Format(CultureInfo.CurrentCulture, DicomCoreResource.UnsupportedSearchParameter, "Modality"));
    }

    [Fact]
    public async Task GivenSearchRequest_WithValidParamsAndNoMatchingResult_ReturnNoContent()
    {
        using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _client.QueryWorkitemAsync("PatientID=20200101");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task WhenQueryingWorkitem_TheServerShouldReturnWorkitemSuccessfully()
    {
        var workitemUid = TestUidGenerator.Generate();
        DicomDataset dicomDataset = Samples.CreateRandomWorkitemInstanceDataset(workitemUid);
        dicomDataset.AddOrUpdate(DicomTag.PatientName, "Foo");

        using DicomWebResponse response = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);

        Assert.True(response.IsSuccessStatusCode);

        using DicomWebAsyncEnumerableResponse<DicomDataset> queryResponse = await _client.QueryWorkitemAsync("PatientName=Foo");

        Assert.Equal(KnownContentTypes.ApplicationDicomJson, queryResponse.ContentHeaders.ContentType.MediaType);
        DicomDataset[] datasets = await queryResponse.ToArrayAsync();

        Assert.NotNull(datasets);
        DicomDataset testDataResponse = datasets.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.PatientName) == "Foo");
        Assert.NotNull(testDataResponse);
    }

    [Fact]
    [Trait("Category", "bvt")]
    public async Task WhenQueryingWorkitemWithFilter_TheServerShouldReturnWorkitemSuccessfully()
    {
        var workitemUid = TestUidGenerator.Generate();
        DicomDataset dicomDataset = Samples.CreateRandomWorkitemInstanceDataset(workitemUid);

        using DicomWebResponse response = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);

        Assert.True(response.IsSuccessStatusCode);

        using DicomWebAsyncEnumerableResponse<DicomDataset> queryResponse = await _client.QueryWorkitemAsync("ProcedureStepState=SCHEDULED");

        Assert.Equal(KnownContentTypes.ApplicationDicomJson, queryResponse.ContentHeaders.ContentType.MediaType);
        DicomDataset[] datasets = await queryResponse.ToArrayAsync();

        Assert.NotNull(datasets);
        DicomDataset testDataResponse = datasets.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.ProcedureStepState) == "SCHEDULED");
        Assert.NotNull(testDataResponse);
    }

    [Fact]
    public async Task WhenQueryingWorkitemWithSequenceMatching_TheServerShouldReturnWorkitemSuccessfully()
    {
        var workitemUid = TestUidGenerator.Generate();
        DicomDataset dicomDataset = Samples.CreateRandomWorkitemInstanceDataset(workitemUid);
        string codeValue = "testCodeVal";
        var dataset = new DicomDataset();
        dataset.Add(DicomTag.CodeValue, codeValue);
        dataset.Add(DicomTag.CodeMeaning, "testCodeMeaning");
        dicomDataset.AddOrUpdate(DicomTag.ScheduledStationNameCodeSequence, dataset);

        using DicomWebResponse response = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);

        Assert.True(response.IsSuccessStatusCode);

        using DicomWebAsyncEnumerableResponse<DicomDataset> queryResponse = await _client.QueryWorkitemAsync($"ScheduledStationNameCodeSequence.CodeValue={codeValue}");

        Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);
        Assert.Equal(KnownContentTypes.ApplicationDicomJson, queryResponse.ContentHeaders.ContentType.MediaType);
        DicomDataset[] datasets = await queryResponse.ToArrayAsync();

        Assert.NotNull(datasets);
        DicomSequence sequence = datasets.FirstOrDefault()?.GetSequence(DicomTag.ScheduledStationNameCodeSequence);
        Assert.NotNull(sequence);

        var actualValue = sequence.FirstOrDefault(ds => ds.GetSingleValue<string>(DicomTag.CodeValue) == codeValue);
        Assert.NotNull(actualValue);
    }

    [Fact]
    public async Task GivenSearchRequest_PatientNameFuzzyMatch_MatchResult()
    {
        var workitemUid = TestUidGenerator.Generate();
        var randomNamePart = Guid.NewGuid().ToString("N").Substring(0, 14).ToUpper();
        DicomDataset dicomDataset = Samples.CreateRandomWorkitemInstanceDataset(workitemUid);
        dicomDataset.AddOrUpdate(DicomTag.PatientName, $"Jonathan^{randomNamePart}^Stone Hall^^");

        using DicomWebResponse response = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);

        Assert.True(response.IsSuccessStatusCode);

        DicomDataset testDataResponse = null;
        DicomDataset[] responseDatasets = null;
        int retryCount = 0;
        while (retryCount < 3 || testDataResponse == null)
        {
            using DicomWebAsyncEnumerableResponse<DicomDataset> queryResponse = await _client.QueryWorkitemAsync($"PatientName={randomNamePart}&FuzzyMatching=true");
            if (response.ContentHeaders.ContentType != null)
            {
                Assert.Equal(KnownContentTypes.ApplicationJson, queryResponse.ContentHeaders.ContentType.MediaType);
            }
            responseDatasets = await queryResponse.ToArrayAsync();

            testDataResponse = responseDatasets?.FirstOrDefault();
            retryCount++;
        }

        Assert.NotNull(testDataResponse);
        Assert.Equal(dicomDataset.GetSingleValue<string>(DicomTag.PatientName), testDataResponse.GetSingleValue<string>(DicomTag.PatientName));
    }

    [Fact]
    public async Task GivenSearchRequest_WithHigherLimit_ReturnBadRequest()
    {
        DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
            () => _client.QueryWorkitemAsync("PatientName=Foo&limit=500"));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.InvalidQueryStringValue, "Limit", HttpUtility.UrlEncode("The field Limit must be between 1 and 200.")), exception.ResponseMessage);
    }
}
