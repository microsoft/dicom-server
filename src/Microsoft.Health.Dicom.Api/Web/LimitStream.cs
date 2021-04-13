// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Api.Web
{
    internal class LimitStream : Stream
    {
        private readonly Stream _stream;

        private long _bytesLeft;

        private readonly long _limit;

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position { get => _stream.Position; set => throw new NotSupportedException(); }

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
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }
    }
}
