// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    internal class LazyTransformStream<T> : Stream
    {
        private readonly object _lockObject = new object();
        private readonly Func<T, Stream> _transformFunction;
        private readonly T _transformInput;
        private Stream _outputStream;

        public LazyTransformStream(T transformInput, Func<T, Stream> transformFunction)
        {
            EnsureArg.IsNotNull(transformFunction, nameof(transformFunction));

            _transformInput = transformInput;
            _transformFunction = transformFunction;
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
                        _outputStream = _transformFunction(_transformInput);
                    }
                }
            }

            return _outputStream;
        }
    }
}
