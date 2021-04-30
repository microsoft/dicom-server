// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Api.Web
{
    /// <summary>
    /// Provides functionality to convert stream into a seekable stream.
    /// </summary>
    internal class SeekableStreamConverter : ISeekableStreamConverter
    {
        private const int DefaultBufferThreshold = 1024 * 30000; // 30MB
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SeekableStreamConverter(IHttpContextAccessor httpContextAccessor)
        {
            EnsureArg.IsNotNull(httpContextAccessor, nameof(httpContextAccessor));

            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public async Task<Stream> ConvertAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(stream, nameof(stream));

            int bufferThreshold = DefaultBufferThreshold;
            long? bufferLimit = null;
            Stream seekableStream = null;

            if (!stream.CanSeek)
            {
                seekableStream = new FileBufferingReadStream(stream, bufferThreshold, bufferLimit, AspNetCoreTempDirectory.TempDirectoryFactory);
                _httpContextAccessor.HttpContext?.Response.RegisterForDisposeAsync(seekableStream);
                await seekableStream.DrainAsync(cancellationToken);
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
                    string temp = Environment.GetEnvironmentVariable("ASPNETCORE_TEMP") ?? // ASPNETCORE_TEMP - User set temporary location.
                                  Path.GetTempPath();                                      // Fall back.

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
}
