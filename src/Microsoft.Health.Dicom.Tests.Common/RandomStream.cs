// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using EnsureThat;

namespace Microsoft.Health.Dicom.Tests.Common;

internal sealed class RandomStream : Stream
{
    private long _position = 0;
    private Random _random;
    private readonly int _seed;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length { get; }

    public override long Position
    {
        get => _position;
        set
        {
            // Position cannot be moved
            if (value == 0)
            {
                if (_position != 0)
                {
                    _position = 0;
                    _random = new Random(_seed);
                }
            }
            else if (_position != value)
            {
                throw new InvalidOperationException();
            }
        }
    }

    public RandomStream(long length, int? seed = null)
    {
        Length = EnsureArg.IsGte(length, 0, nameof(length));
        _seed = seed ?? Thread.CurrentThread.ManagedThreadId;
        _random = new Random(_seed);
    }

    public override void Flush()
    { }

    public override int Read(byte[] buffer, int offset, int count)
        => Read(new Span<byte>(buffer, offset, count));

    public override int Read(Span<byte> buffer)
    {
        int len = (int)Math.Min(buffer.Length, Length - Position);
        if (buffer.Length != len)
            buffer = buffer[..len];

        // Fill up the buffer with random bytes
        _position += len;
        _random.NextBytes(buffer);
        return len;
    }

    public override int ReadByte()
    {
        if (Position >= Length)
            return -1;

        _position++;
        return _random.Next(byte.MinValue, byte.MaxValue);
    }

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();
}
