// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Api.Features.ByteCounter
{
    /// <summary>
    /// A stream to wrap an underlying stream and counts the number of bytes passed while operating with it.
    /// </summary>
    public class ByteCountingStream : Stream
    {
        private readonly Stream _stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteCountingStream"/> class.
        /// </summary>
        /// <param name="stream">An underlying stream.</param>
        public ByteCountingStream(Stream stream)
        {
            EnsureArg.IsNotNull(stream, nameof(stream));

            _stream = stream;
        }

        /// <inheritdoc />
        public override bool CanRead => _stream.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => _stream.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => _stream.CanWrite;

        /// <inheritdoc />
        public override long Length => _stream.Length;

        /// <inheritdoc />
        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        /// <summary>
        /// The number of bytes that have been written.
        /// </summary>
        public long WrittenByteCount { get; private set; }

        /// <inheritdoc />
        public override void Flush()
        {
            _stream.Flush();
        }

        /// <inheritdoc />
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _stream.FlushAsync(cancellationToken);
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        /// <inheritdoc />
        public override void WriteByte(byte value)
        {
            _stream.WriteByte(value);
            WrittenByteCount++;
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
            WrittenByteCount += count;
        }

        /// <summary>
        /// Asynchronously writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">The zero-based byte offset in buffer from which to begin copying bytes to the stream.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public new Task WriteAsync(byte[] buffer, int offset, int count)
        {
            WrittenByteCount += count;
            return _stream.WriteAsync(buffer, offset, count);
        }

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            WrittenByteCount += count;
            return _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc />
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            WrittenByteCount += count;
            return _stream.BeginWrite(buffer, offset, count, callback, state);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            _stream.Dispose();
            base.Dispose(disposing);
        }
    }
}
