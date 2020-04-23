// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Api.Web
{
    /// <summary>
    /// Provides functionality to convert stream into a seekable stream.
    /// </summary>
    internal class MultipartReaderStreamToSeekableStreamConverter : ISeekableStreamConverter
    {
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public MultipartReaderStreamToSeekableStreamConverter(RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        }

        /// <inheritdoc />
        public async Task<Stream> ConvertAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(stream, nameof(stream));

            Stream seekableStream = null;

            try
            {
                seekableStream = _recyclableMemoryStreamManager.GetStream();

                try
                {
                    await stream.CopyToAsync(seekableStream, cancellationToken);
                }
                catch (IOException ex)
                {
                    throw new InvalidMultipartBodyPartException(ex);
                }

                seekableStream.Seek(0, SeekOrigin.Begin);

                return seekableStream;
            }
            catch (Exception)
            {
                if (seekableStream != null)
                {
                    await seekableStream.DisposeAsync();
                }

                throw;
            }
        }
    }
}
