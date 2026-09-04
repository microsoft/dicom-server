// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Xunit;

namespace Microsoft.Health.Dicom.Client.UnitTests;

public class RetrieveStudyMetadataInfinityTests
{
    [Fact]
    public async Task GivenFlMetadataWithNegativeInfinity_WhenRetrieveStudyMetadataAsync_DoesNotThrowAndParsesValues()
    {
        const string json = """
            [
              {
                "00720076": {
                  "vr": "FL",
                  "Value": ["-Infinity", -1]
                }
              }
            ]
            """;

        using HttpClient httpClient = CreateClient(json);
        var client = new DicomWebClient(httpClient);

        DicomWebAsyncEnumerableResponse<DicomDataset> response = await client.RetrieveStudyMetadataAsync("1.2.3.4.5");
        List<DicomDataset> datasets = await ToListAsync(response);

        Assert.Single(datasets);
        float[] values = datasets[0].GetValues<float>(DicomTag.SelectorFLValue);
        Assert.Equal(2, values.Length);
        Assert.True(float.IsNegativeInfinity(values[0]));
        Assert.Equal(-1f, values[1]);
    }

    [Fact]
    public async Task GivenFlMetadataWithFiniteNumber_WhenRetrieveStudyMetadataAsync_ParsesValues()
    {
        const string json = """
            [
              {
                "00720076": {
                  "vr": "FL",
                  "Value": [-1]
                }
              }
            ]
            """;

        using HttpClient httpClient = CreateClient(json);
        var client = new DicomWebClient(httpClient);

        DicomWebAsyncEnumerableResponse<DicomDataset> response = await client.RetrieveStudyMetadataAsync("1.2.3.4.5");
        List<DicomDataset> datasets = await ToListAsync(response);

        Assert.Single(datasets);
        Assert.Equal(-1f, datasets[0].GetSingleValue<float>(DicomTag.SelectorFLValue));
    }

    private static HttpClient CreateClient(string json)
    {
        return new HttpClient(new StubMetadataHandler(json))
        {
            BaseAddress = new System.Uri("https://dicom.test/"),
        };
    }

    private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
    {
        var items = new List<T>();
        await foreach (T item in source)
        {
            items.Add(item);
        }

        return items;
    }

    private sealed class StubMetadataHandler : HttpMessageHandler
    {
        private readonly string _json;

        public StubMetadataHandler(string json)
        {
            _json = json;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, Encoding.UTF8),
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/dicom+json");
            return Task.FromResult(response);
        }
    }
}
