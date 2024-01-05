// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Extensions;

public class HttpRequestExtensionsTests
{
    [Fact]
    public void GivenEmptyHeaders_WhenGetAcceptHeaders_ThenShouldReturnEmpty()
    {
        var httpRequest = Substitute.For<HttpRequest>();
        IHeaderDictionary headers = new HeaderDictionary();
        httpRequest.Headers.Returns(headers);
        IEnumerable<AcceptHeader> acceptHeaders = httpRequest.GetAcceptHeaders();
        Assert.Empty(acceptHeaders);
    }

    [Fact]
    public void GivenNonEmptyHeaders_WhenGetAcceptHeaders_ThenShouldReturnHeaders()
    {
        var httpRequest = Substitute.For<HttpRequest>();
        HeaderDictionary headers = new() { { "accept", "application/dicom" } };
        httpRequest.Headers.Returns(headers);
        IEnumerable<AcceptHeader> acceptHeaders = httpRequest.GetAcceptHeaders();
        Assert.Single(acceptHeaders);
    }

    [Fact]
    public void GivenNullHttpRequest_WhenGetAcceptHeaders_ThenShouldThrowException()
    {
        Assert.Throws<ArgumentNullException>(() => HttpRequestExtensions.GetAcceptHeaders(null));
    }

    [Fact]
    public void GivenHttpRequest_WhenGetHostAndFollowDicomStandard_ThenShouldReturnExpectedValue()
    {
        string host = "host1";
        var httpRequest = Substitute.For<HttpRequest>();
        httpRequest.Host.Returns(new HostString(host));
        Assert.Equal(host + ":", httpRequest.GetHost(dicomStandards: true));
    }

    [Fact]
    public void GivenHttpRequestHavingNoHost_WhenGetHost_ThenShouldReturnExpectedValue()
    {
        var httpRequest = Substitute.For<HttpRequest>();
        httpRequest.Host.Returns(new HostString(string.Empty));
        Assert.Equal(string.Empty, httpRequest.GetHost(dicomStandards: true));
    }

    [Fact]
    public void GivenHttpRequest_WhenGetHostNotFollowingDicomStandard_ThenShouldReturnExpectedValue()
    {
        string host = "host1";
        var httpRequest = Substitute.For<HttpRequest>();
        httpRequest.Host.Returns(new HostString(host));
        Assert.Equal(host, httpRequest.GetHost(dicomStandards: false));
    }

    [Fact]
    public void GivenHttpRequestWithOriginalHeader_WhenGetIsOriginalVersionRequested_ThenShouldReturnExpectedValue()
    {
        HttpRequest httpRequest = Substitute.For<HttpRequest>();
        IHeaderDictionary headers = new HeaderDictionary
        {
            { "accept", "application/dicom" },
            { "msdicom-request-original", bool.TrueString }
        };
        httpRequest.Headers.Returns(headers);
        Assert.True(httpRequest.IsOriginalVersionRequested());
    }

    [Fact]
    public void GivenHttpRequestWithNoOriginalHeader_WhenGetIsOriginalVersionRequested_ThenShouldReturnExpectedValue()
    {
        HttpRequest httpRequest = Substitute.For<HttpRequest>();
        IHeaderDictionary headers = new HeaderDictionary
        {
            { "accept", "application/dicom" }
        };
        httpRequest.Headers.Returns(headers);
        Assert.False(httpRequest.IsOriginalVersionRequested());
    }

    [Fact]
    public void GivenHttpRequestWithEmptyOriginalHeader_WhenGetIsOriginalVersionRequested_ThenShouldReturnExpectedValue()
    {
        HttpRequest httpRequest = Substitute.For<HttpRequest>();
        IHeaderDictionary headers = new HeaderDictionary
        {
            { "accept", "application/dicom" },
            { "msdicom-request-original", string.Empty }
        };
        httpRequest.Headers.Returns(headers);
        Assert.False(httpRequest.IsOriginalVersionRequested());
    }
}
