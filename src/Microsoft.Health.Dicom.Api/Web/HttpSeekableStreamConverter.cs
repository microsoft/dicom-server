// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.WebUtilities;

namespace Microsoft.Health.Dicom.Api.Web;

public class HttpSeekableStreamConverter : SeekableStreamConverter
{
    private string _tempDirectory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpSeekableStreamConverter(IHttpContextAccessor httpContextAccessor, ILogger<SeekableStreamConverter> logger) : base(logger)
    {
        _httpContextAccessor = EnsureArg.IsNotNull(httpContextAccessor, nameof(httpContextAccessor));
    }

    protected override void RegisterForDispose(Stream stream)
    {
        _httpContextAccessor.HttpContext?.Response.RegisterForDisposeAsync(stream);
    }

    protected override long GetContentLength()
    {
        return _httpContextAccessor.HttpContext?.Request.ContentLength ?? 0;
    }

    protected override string GetTempDirectory()
    {
        if (_tempDirectory != null)
        {
            return _tempDirectory;
        }

        // Look for folders in the following order.
        // ASPNETCORE_TEMP - User set temporary location.
        string temp = Environment.GetEnvironmentVariable("ASPNETCORE_TEMP") ?? Path.GetTempPath();

        if (!Directory.Exists(temp))
        {
            throw new DirectoryNotFoundException(temp);
        }

        _tempDirectory = temp;

        return _tempDirectory;
    }
}
