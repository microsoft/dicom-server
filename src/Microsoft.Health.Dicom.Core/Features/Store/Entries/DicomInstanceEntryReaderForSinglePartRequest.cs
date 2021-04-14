// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Web;
using NotSupportedException = System.NotSupportedException;

namespace Microsoft.Health.Dicom.Core.Features.Store.Entries
{
    /// <summary>
    /// Provides functionality to read DICOM instance entries from HTTP application/dicom request.
    /// </summary>
    public class DicomInstanceEntryReaderForSinglePartRequest : IDicomInstanceEntryReader
    {
        private readonly ISeekableStreamConverter _seekableStreamConverter;
        private readonly IOptions<StoreConfiguration> _storeConfiguration;


        public DicomInstanceEntryReaderForSinglePartRequest(ISeekableStreamConverter seekableStreamConverter, IOptions<StoreConfiguration> storeConfiguration)
        {
            EnsureArg.IsNotNull(seekableStreamConverter, nameof(seekableStreamConverter));
            EnsureArg.IsNotNull(storeConfiguration, nameof(storeConfiguration));

            _seekableStreamConverter = seekableStreamConverter;
            _storeConfiguration = storeConfiguration;
        }

        /// <inheritdoc />
        public bool CanRead(string contentType)
        {
            return MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue media) &&
                string.Equals(KnownContentTypes.ApplicationDicom, media.MediaType, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<IDicomInstanceEntry>> ReadAsync(string contentType, Stream stream, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(contentType, nameof(contentType));
            EnsureArg.IsNotNull(stream, nameof(stream));

            var dicomInstanceEntries = new List<StreamOriginatedDicomInstanceEntry>();

            if (!KnownContentTypes.ApplicationDicom.Equals(contentType, StringComparison.OrdinalIgnoreCase))
            {
                // TODO: Currently, we only support application/dicom. Support for metadata + bulkdata is coming.
                throw new UnsupportedMediaTypeException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.UnsupportedContentType, contentType));
            }

            Stream seekableStream;
            using (Stream limitStream = new LimitStream(stream, _storeConfiguration.Value.MaxAllowedDicomFileSize))
            {
                seekableStream = await _seekableStreamConverter.ConvertAsync(limitStream, cancellationToken);
            }

            dicomInstanceEntries.Add(new StreamOriginatedDicomInstanceEntry(seekableStream));

            return dicomInstanceEntries;
        }

        private class LimitStream : Stream
        {
            private readonly Stream _stream;

            private long _bytesLeft;

            private readonly long _limit;

            public override bool CanRead => _stream.CanRead;

            public override bool CanSeek => false;

            public override bool CanWrite => _stream.CanWrite;

            public override long Length => throw new NotSupportedException();

            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public LimitStream(Stream stream, long limit)
            {
                EnsureArg.IsNotNull(stream, nameof(stream));
                EnsureArg.IsGte(limit, 0);

                _stream = stream;
                _bytesLeft = limit;
                _limit = limit;
            }

            public override void Flush()
            {
                _stream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                ThrowIfExceedLimit();

                int amountRead = _stream.Read(buffer, offset, count);
                _bytesLeft -= amountRead;

                ThrowIfExceedLimit();

                return amountRead;
            }

            [SuppressMessage("Performance", "CA1835:Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'", Justification = "Buffer is pass through")]
            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ThrowIfExceedLimit();

                int amountRead = await _stream.ReadAsync(buffer, offset, count, cancellationToken);
                _bytesLeft -= amountRead;

                ThrowIfExceedLimit();

                return amountRead;
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                ThrowIfExceedLimit();

                int amountRead = await _stream.ReadAsync(buffer, cancellationToken);
                _bytesLeft -= amountRead;

                ThrowIfExceedLimit();

                return amountRead;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void ThrowIfExceedLimit()
            {
                if (_bytesLeft < 0)
                {
                    throw new DicomFileLengthLimitExceededException(_limit);
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _stream.Write(buffer, offset, count);
            }
        }
    }
}
