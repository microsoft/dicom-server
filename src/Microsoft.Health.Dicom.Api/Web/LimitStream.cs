// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Api.Web
{
    class LimitStream : Stream
    {
        private Stream _stream;

        private long _bytesLeft;

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position { get => _stream.Position; set => throw new NotImplementedException(); }

        public LimitStream(Stream stream, long limit)
        {
            EnsureArg.IsNotNull(stream, nameof(stream));

            _stream = stream;
            _bytesLeft = limit;
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_bytesLeft <= 0)
            {
                throw new DicomFileLengthLimitExceededException(_bytesLeft);
            }

            int amountRead = _stream.Read(buffer, offset, count);

            _bytesLeft -= amountRead;
            makeCheck();

            return amountRead;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1835:Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'", Justification = "Buffer is pass through")]
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_bytesLeft <= 0)
            {
                throw new DicomFileLengthLimitExceededException(_bytesLeft);
            }

            int amountRead = await _stream.ReadAsync(buffer, offset, count, cancellationToken);

            _bytesLeft -= amountRead;
            makeCheck();

            return amountRead;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_bytesLeft <= 0)
            {
                throw new DicomFileLengthLimitExceededException(_bytesLeft);
            }

            int amountRead = await _stream.ReadAsync(buffer, cancellationToken);

            _bytesLeft -= amountRead;
            makeCheck();

            return amountRead;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void makeCheck()
        {
            if (_bytesLeft < 0)
            {
                throw new DicomFileLengthLimitExceededException(_bytesLeft);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
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
