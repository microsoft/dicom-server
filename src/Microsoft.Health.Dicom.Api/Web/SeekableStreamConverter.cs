// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
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
    private const int DefaultBufferThreshold = 1024 * 1024; // 1MB
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SeekableStreamConverter> _logger;

    public SeekableStreamConverter(IHttpContextAccessor httpContextAccessor, ILogger<SeekableStreamConverter> logger)
    {
        _httpContextAccessor = EnsureArg.IsNotNull(httpContextAccessor, nameof(httpContextAccessor));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    /// <inheritdoc />
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Caller will dipose of Stream.")]
    public async Task<Stream> ConvertAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(stream, nameof(stream));

        Stream seekableStream = null;

        if (!stream.CanSeek)
        {
            long? bufferLimit = null;
            // https://github.com/dotnet/aspnetcore/blob/main/src/Http/WebUtilities/src/FileBufferingReadStream.cs
            // FileBufferingReadStream uses memory buffer of shared byte array pool if memoryThreshold is <= 1MB. Otherwise it is using MemoryStream
            // which is not efficient memory reuse.
            // Current logic works like below
            // If dcm file is > 1MB, drain it to file and return filestream since memoryBufferThreshold is set to 0
            // else use the memory buffer of <= 1MB created with bytePool.Rent(memoryThreshold) to create the seekable stream
            int memoryBufferThreshold = DefaultBufferThreshold;
            if (_httpContextAccessor.HttpContext?.Request.ContentLength > DefaultBufferThreshold)
            {
                memoryBufferThreshold = 0;
            }
            _logger.LogInformation("Request content length {requestContentLength}", _httpContextAccessor.HttpContext?.Request.ContentLength);
            seekableStream = new FileBufferingReadStream(stream, memoryBufferThreshold, bufferLimit, AspNetCoreTempDirectory.TempDirectoryFactory);
            _httpContextAccessor.HttpContext?.Response.RegisterForDisposeAsync(seekableStream);
            await seekableStream.DrainAsync(cancellationToken);
            _logger.LogInformation("Finished draining request body");
        }
        else
        {
            seekableStream = stream;
        }

        seekableStream.Seek(0, SeekOrigin.Begin);

        return seekableStream;
    }

    private static class AspNetCoreTempDirectory
    {
        private static string s_tempDirectory;

        public static Func<string> TempDirectoryFactory => GetTempDirectory;

        private static string GetTempDirectory()
        {
            if (s_tempDirectory == null)
            {
                // Look for folders in the following order.
                // ASPNETCORE_TEMP - User set temporary location.
                string temp = Environment.GetEnvironmentVariable("ASPNETCORE_TEMP") ?? Path.GetTempPath();

                if (!Directory.Exists(temp))
                {
                    throw new DirectoryNotFoundException(temp);
                }

                s_tempDirectory = temp;
            }

            return s_tempDirectory;
        }
    }
}
