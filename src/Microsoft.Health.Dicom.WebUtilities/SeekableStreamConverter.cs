// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using EnsureThat;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.WebUtilities;

/// <summary>
/// Provides functionality to convert stream into a seekable stream.
/// </summary>
public abstract class SeekableStreamConverter : ISeekableStreamConverter
{
    private const int DefaultBufferThreshold = 1024 * 1024; // 1MB
    private readonly ILogger<SeekableStreamConverter> _logger;

    protected SeekableStreamConverter(ILogger<SeekableStreamConverter> logger)
    {
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    /// <inheritdoc />
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Caller will dipose of Stream.")]
    public async Task<Stream> ConvertAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(stream, nameof(stream));

        Stream seekableStream;

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
            long contentLength = GetContentLength();

            if (contentLength > DefaultBufferThreshold)
            {
                memoryBufferThreshold = 0;
            }
            _logger.LogInformation("Request content length {requestContentLength}", contentLength);
            seekableStream = new FileBufferingReadStream(stream, memoryBufferThreshold, bufferLimit, GetTempDirectory());
            RegisterForDispose(seekableStream);
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

    protected abstract void RegisterForDispose(Stream stream);

    protected abstract long GetContentLength();

    protected abstract string GetTempDirectory();
}
