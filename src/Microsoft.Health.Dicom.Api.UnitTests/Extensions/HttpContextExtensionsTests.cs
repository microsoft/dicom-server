// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Health.Dicom.Api.Extensions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Extensions;

public class HttpContextExtensionsTests
{
    private readonly HttpContext _context;
    private readonly IApiVersioningFeature _apiVersioningFeature = Substitute.For<IApiVersioningFeature>();

    public HttpContextExtensionsTests()
    {
        _context = new DefaultHttpContext();
        _context.Features.Set(_apiVersioningFeature);
    }

    [Fact]
    public void GivenNoVersioningFeature_WhenGettingMajorVersion_ThenReturnDefault()
        => Assert.Equal(1, _context.GetMajorRequestedApiVersion());

    [Fact]
    public void GivenNoVersion_WhenGettingMajorVersion_ThenReturnDefault()
    {
        _apiVersioningFeature.RequestedApiVersion.Returns((ApiVersion)null);
        Assert.Equal(1, _context.GetMajorRequestedApiVersion());
    }

    [Theory]
    [InlineData("1.0-prerelease", 1)]
    [InlineData("1.0", 1)]
    [InlineData("2.3", 2)]
    [InlineData("56.78", 56)]
    public void GivenApiVersion_WhenGettingMajorVersion_ThenParseVersion(string version, int expected)
    {
        _apiVersioningFeature.RequestedApiVersion.Returns(ApiVersion.Parse(version));
        Assert.Equal(expected, _context.GetMajorRequestedApiVersion());
    }
}
