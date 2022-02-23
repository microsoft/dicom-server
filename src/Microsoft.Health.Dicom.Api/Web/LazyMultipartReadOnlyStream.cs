// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Api.Web
{
    // inspired by
    // https://github.com/microsoft/referencesource/blob/master/System/net/System/Net/Http/MultipartContent.cs
    // https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/storage/Azure.Storage.Common/src/Shared/LazyLoadingReadOnlyStream.cs
#pragma warning disable CA1844 // Provide memory-based overrides of async methods when subclassing 'Stream'
    public class LazyMultipartReadOnlyStream : Stream
#pragma warning restore CA1844 // Provide memory-based overrides of async methods when subclassing 'Stream'
    {
#pragma warning disable CA2213 // Disposable fields should be disposed, (disposed in ReadAsync)
        private readonly IAsyncEnumerator<DicomStreamContent> _asyncEnumerator;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly int _bufferSize;
        private const string Crlf = "\r\n";
        private readonly Encoding _defaultHttpEncoding = Encoding.GetEncoding(28591);
        private readonly string _boundary;
        private readonly string _intermediateBoundary;
        private const int KB = 1024;

        private byte[] _buffer;
        private DicomStreamContent _currentStreamContent;
        private int _bufferPosition;
        private int _bufferLength;
        private bool _terminating;

        public LazyMultipartReadOnlyStream(
            IAsyncEnumerable<DicomStreamContent> enumerableStreams,
            string boundary,
            int bufferSize,
            CancellationToken cancellation)
            : base()
        {
            EnsureArg.IsNotNull(enumerableStreams, nameof(enumerableStreams));
            EnsureArg.IsNotEmptyOrWhiteSpace(boundary, nameof(boundary));
            EnsureArg.IsGt(bufferSize, KB, nameof(bufferSize));

            _asyncEnumerator = enumerableStreams.GetAsyncEnumerator(cancellation);
            _bufferSize = bufferSize;
            _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            _boundary = boundary;
            _intermediateBoundary = Crlf + "--" + _boundary + Crlf;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        #region NotImplemented Overrides
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
        public override long Length => throw new NotImplementedException();
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
        #endregion

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);

            // check
            if (_bufferPosition == _bufferLength && _terminating)
            {
                return 0;
            }

            // initialize
            if (_currentStreamContent == null)
            {
                if (await _asyncEnumerator.MoveNextAsync())
                {
                    _currentStreamContent = _asyncEnumerator.Current;
                    CopyStringToBuffer(GetHeaderBoundary(true));
                }
                else
                {
                    throw new InvalidOperationException("Enumerator cannot be empty");
                }
            }

            if (_bufferPosition == _bufferLength && _currentStreamContent.Stream.Position == _currentStreamContent.Stream.Length)
            {
                if (await _asyncEnumerator.MoveNextAsync())
                {
                    await _currentStreamContent.Stream.DisposeAsync();
                    _currentStreamContent = _asyncEnumerator.Current;
                    CopyStringToBuffer(GetHeaderBoundary(false));
                }
                else
                {
                    await _currentStreamContent.Stream.DisposeAsync();
                    CopyStringToBuffer(GetTerminatingBoundary());
                    _terminating = true;
                }
            }

            // get
            if (!_terminating && _bufferPosition == _bufferLength)
            {
#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync', (Azure.Storage.LazyLoadingReadOnlyStream does not implement that.)
                _bufferLength = await _currentStreamContent.Stream.ReadAsync(_buffer, 0, _bufferSize, cancellationToken);
#pragma warning restore CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
                _bufferPosition = 0;
            }

            // We will return the minimum of remainingBytesInBuffer and the count provided by the user
            int remainingBytesInBuffer = _bufferLength - _bufferPosition;
            int bytesToWrite = Math.Min(remainingBytesInBuffer, count);

            // copy
            Array.Copy(_buffer, _bufferPosition, buffer, offset, bytesToWrite);
            _bufferPosition += bytesToWrite;

            return bytesToWrite;
        }

        private void CopyStringToBuffer(string multipartHeader)
        {
            byte[] startBoundary = EncodeStringToByteArray(multipartHeader);
            int bytesToWrite = startBoundary.Length;
            Array.Copy(startBoundary, 0, _buffer, 0, bytesToWrite);
            _bufferPosition = 0;
            _bufferLength = bytesToWrite;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            // Return the buffer to the pool if we're called from Dispose or a finalizer
            if (_buffer != null)
            {
                ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
                _buffer = null;
            }
        }

        #region MultiPart boundary write
        private string GetHeaderBoundary(bool isStart)
        {
            var multiPartStringBuilder = new StringBuilder();
            if (isStart)
            {
                multiPartStringBuilder.Append(GetStartBoundary());

            }
            else
            {
                multiPartStringBuilder.Append(GetIntermediateBoundary());
            }
            AppendContentHeader(multiPartStringBuilder, _currentStreamContent.Headers);
            return multiPartStringBuilder.ToString();
        }

        private byte[] EncodeStringToByteArray(string input)
        {
            return _defaultHttpEncoding.GetBytes(input);
        }

        private string GetStartBoundary()
        {
            return "--" + _boundary + Crlf;
        }

        private string GetIntermediateBoundary()
        {
            return _intermediateBoundary;
        }

        private string GetTerminatingBoundary()
        {
            return Crlf + "--" + _boundary + "--" + Crlf;
        }

        private static void AppendContentHeader(StringBuilder builder, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> headerPair in headers)
            {
                builder.Append(headerPair.Key + ": " + string.Join(", ", headerPair.Value) + Crlf);
            }
            builder.Append(Crlf);
        }
        #endregion
    }
}
