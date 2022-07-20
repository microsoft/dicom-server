// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Api.Web;

/// <summary>
/// Provides functionality to convert stream into a seekable stream.
/// </summary>
internal class SeekableStreamConverter : ISeekableStreamConverter
{
    private const int DefaultBufferThreshold = 1024 * 30000; // 30MB
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ISeekableStreamConverter> _logger;

    public SeekableStreamConverter(IHttpContextAccessor httpContextAccessor, ILogger<ISeekableStreamConverter> logger)
    {
        EnsureArg.IsNotNull(httpContextAccessor, nameof(httpContextAccessor));
        _logger = logger;

        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public async Task<Stream> ConvertAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(stream, nameof(stream));

        int bufferThreshold = DefaultBufferThreshold;
        long? bufferLimit = null;
        Stream seekableStream = null;

        var stop = new Stopwatch();
        stop.Start();

        if (!stream.CanSeek)
        {
            // seekableStream = new AspNetCore.WebUtilities.FileBufferingReadStream(stream, bufferThreshold, bufferLimit, AspNetCoreTempDirectory.TempDirectoryFactory);
            seekableStream = new FileBufferingReadStream(stream, bufferThreshold, bufferLimit, AspNetCoreTempDirectory.TempDirectoryFactory);
            _httpContextAccessor.HttpContext?.Response.RegisterForDisposeAsync(seekableStream);
            await (seekableStream as FileBufferingReadStream).BufferAsync(cancellationToken);
            // await seekableStream.DrainAsync(cancellationToken);
        }
        else
        {
            seekableStream = stream;
        }

        stop.Stop();

        _logger.LogInformation("Total time take to ConvertAsync: New Code : {ElapsedMilliseconds} ms", stop.ElapsedMilliseconds);
        // _logger.LogInformation("Total time take to ConvertAsync: New Code : {ElapsedMilliseconds} ms", stop.ElapsedMilliseconds);

        seekableStream.Seek(0, SeekOrigin.Begin);

        return seekableStream;
    }
}
