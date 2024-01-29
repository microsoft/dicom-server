// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Azure.Core;
using Azure.Core.Pipeline;
using Microsoft.Health.Dicom.Blob.Features.ExternalStore;
using Microsoft.Health.Dicom.Blob.Utilities;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests.Features.ExternalStore;

public class ExternalStoreHealthExpiryHttpPolicyTests
{
    private readonly ExternalBlobDataStoreConfiguration _blobDataStoreConfiguration;
    private readonly ExternalStoreHealthExpiryHttpPipelinePolicy _externalStoreHealthExpiryPolicy;

    private readonly MockRequest _request;
    private readonly HttpPipelinePolicy _mockPipeline = Substitute.For<HttpPipelinePolicy>();

    public ExternalStoreHealthExpiryHttpPolicyTests()
    {
        _blobDataStoreConfiguration = new ExternalBlobDataStoreConfiguration()
        {
            BlobContainerUri = new Uri("https://myBlobStore.blob.core.net/myContainer"),
            StorageDirectory = "DICOM",
            HealthCheckFilePath = "healthCheck/health",
            HealthCheckFileExpiry = TimeSpan.FromMinutes(1),
        };

        _request = new MockRequest();
        _externalStoreHealthExpiryPolicy = new ExternalStoreHealthExpiryHttpPipelinePolicy(_blobDataStoreConfiguration);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("POST")]
    [InlineData("PATCH")]
    public void GivenHealthCheckBlob_Proccess_AddsExpiryHeaders(string requestMethod)
    {
        RequestUriBuilder requestUriBuilder = new RequestUriBuilder();
        requestUriBuilder.Reset(new Uri($"https://myBlobStore.blob.core.net/myContainer/DICOM/healthCheck/health{Guid.NewGuid()}.txt"));

        _request.Uri = requestUriBuilder;
        _request.Method = RequestMethod.Parse(requestMethod);
        HttpMessage httpMessage = new HttpMessage(_request, new ResponseClassifier());

        _externalStoreHealthExpiryPolicy.Process(httpMessage, new ReadOnlyMemory<HttpPipelinePolicy>(new HttpPipelinePolicy[] { _mockPipeline }));

        _request.MockHeaders.Single(h => h.Name == "x-ms-expiry-time" && h.Value == $"{_blobDataStoreConfiguration.HealthCheckFileExpiry.TotalMilliseconds}");
        _request.MockHeaders.Single(h => h.Name == "x-ms-expiry-option" && h.Value == "RelativeToNow");
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("POST")]
    [InlineData("PATCH")]
    [InlineData("GET")]
    [InlineData("DELETE")]
    public void GivenNonHealthCheckBlob_ProccessForAllRequestTypes_NoHeadersAdded(string requestMethod)
    {
        RequestUriBuilder requestUriBuilder = new RequestUriBuilder();
        requestUriBuilder.Reset(new Uri($"https://myBlobStore.blob.core.net/myContainer/DICOM/healthCheck/health{Guid.NewGuid()}.txt/anotherBlob"));

        _request.Uri = requestUriBuilder;
        _request.Method = RequestMethod.Parse(requestMethod);
        HttpMessage httpMessage = new HttpMessage(_request, new ResponseClassifier());

        _externalStoreHealthExpiryPolicy.Process(httpMessage, new ReadOnlyMemory<HttpPipelinePolicy>(new HttpPipelinePolicy[] { _mockPipeline }));

        Assert.Empty(_request.MockHeaders);
    }

    [Theory]
    [InlineData("https://blob.com")]
    [InlineData("https://myBlobStore.blob.core.net/myContainer/DICOM/anotherDirectory/healthCheck/health00000000-0000-0000-0000-000000000000/anotherBlob")]
    [InlineData("https://myBlobStore.blob.core.net/myContainer/DICOM/anotherDirectory/healthCheck/health00000000-0000-0000-0000-000000000000.txt")]
    [InlineData("https://myBlobStore.blob.core.net/myContainer/DICOM/healthCheck/health00000000-0000-0000-0000-000000000000")]
    [InlineData("https://myBlobStore.blob.core.net/myContainer/DICOM/healthCheck/health")]
    [InlineData("https://myBlobStore.blob.core.net/myContainer/DICOM/healthCheck")]
    public void GivenNonHealthCheckBlob_ProccessDifferentBlobUris_NoHeadersAdded(string nonHealthCheckBlob)
    {
        RequestUriBuilder requestUriBuilder = new RequestUriBuilder();
        requestUriBuilder.Reset(new Uri(nonHealthCheckBlob));

        _request.Uri = requestUriBuilder;
        _request.Method = RequestMethod.Put;
        HttpMessage httpMessage = new HttpMessage(_request, new ResponseClassifier());

        _externalStoreHealthExpiryPolicy.Process(httpMessage, new ReadOnlyMemory<HttpPipelinePolicy>(new HttpPipelinePolicy[] { _mockPipeline }));

        Assert.Empty(_request.MockHeaders);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("DELETE")]
    public void GivenHealthCheckBlobReadOrDelete_Proccess_AddsExpiryHeaders(string requestMethod)
    {
        RequestUriBuilder requestUriBuilder = new RequestUriBuilder();
        requestUriBuilder.Reset(new Uri($"https://myBlobStore.blob.core.net/myContainer/DICOM/healthCheck/health{Guid.NewGuid()}.txt"));

        _request.Uri = requestUriBuilder;
        _request.Method = RequestMethod.Parse(requestMethod);
        HttpMessage httpMessage = new HttpMessage(_request, new ResponseClassifier());

        _externalStoreHealthExpiryPolicy.Process(httpMessage, new ReadOnlyMemory<HttpPipelinePolicy>(new HttpPipelinePolicy[] { _mockPipeline }));

        Assert.Empty(_request.MockHeaders);
    }
}
