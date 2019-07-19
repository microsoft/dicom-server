// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    public class LazyTransformStreamStream : Stream
    {
        private readonly object _lockObject = new object();
        private readonly Func<Stream, Stream> _transformStreamFunction;
        private readonly Stream _baseStream;
        private Stream _outputStream;

        public LazyTransformStreamStream(Stream baseStream, Func<Stream, Stream> transformStreamFunction)
        {
            EnsureArg.IsNotNull(baseStream, nameof(baseStream));
            EnsureArg.IsNotNull(transformStreamFunction, nameof(transformStreamFunction));

            _baseStream = baseStream;
            _transformStreamFunction = transformStreamFunction;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => GetOutputStream().Length;

        public override long Position { get => GetOutputStream().Position; set => throw new NotImplementedException(); }

        public override void Flush() => GetOutputStream().Flush();

        public override int Read(byte[] buffer, int offset, int count) => GetOutputStream().Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override void SetLength(long value) => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        private Stream GetOutputStream()
        {
            if (_outputStream == null)
            {
                lock (_lockObject)
                {
                    if (_outputStream == null)
                    {
                        _outputStream = _transformStreamFunction(_baseStream);
                    }
                }
            }

            return _outputStream;
        }
    }
}
