// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.Resources.Retrieve;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Resources.Retrieve
{
    public class LazyTransformReadOnlyStreamTests
    {
        private static readonly byte[] TestData = new byte[] { 1, 2, 3, 4 };

        [Fact]
        public void GivenLazyTransformStream_WhenConstructedWithInvalidArguments_ArgumentExceptionThrown()
        {
            Assert.Throws<ArgumentNullException>(() => new LazyTransformReadOnlyStream<string>(string.Empty, null));
        }

        [Fact]
        public void GivenLazyTransformStream_WhenExecutingWriteMethods_NotSupportedExceptionThrown()
        {
            using (var lazyTransformStream = new LazyTransformReadOnlyStream<byte[]>(new byte[0], ReverseByteArray))
            {
                Assert.Throws<NotSupportedException>(() => lazyTransformStream.SetLength(1));
                Assert.Throws<NotSupportedException>(() => lazyTransformStream.Write(new byte[0], 1, 0));
            }
        }

        [Fact]
        public void GivenLazyTransformStream_WhenConstructed_CanReadSeekButCannotWrite()
        {
            Assert.Throws<ArgumentNullException>(() => new LazyTransformReadOnlyStream<string>(string.Empty, null));

            using (var lazyTransformStream = new LazyTransformReadOnlyStream<byte[]>(new byte[0], ReverseByteArray))
            {
                Assert.True(lazyTransformStream.CanRead);
                Assert.True(lazyTransformStream.CanSeek);
                Assert.False(lazyTransformStream.CanWrite);
            }
        }

        [Fact]
        public void GivenLazyTransformStream_WhenSeeking_IsSetToCorrectStreamPosition()
        {
            using (var lazyTransformStream = new LazyTransformReadOnlyStream<byte[]>(TestData, DoubleByteArray))
            {
                Assert.True(lazyTransformStream.CanSeek);
                Assert.Equal(TestData.Length * 2, lazyTransformStream.Length);
                Assert.Equal(0, lazyTransformStream.Position);

                lazyTransformStream.Seek(2, SeekOrigin.Current);
                Assert.Equal(2, lazyTransformStream.Position);
                lazyTransformStream.Seek(lazyTransformStream.Length - 1, SeekOrigin.Begin);
                Assert.Equal(lazyTransformStream.Length - 1, lazyTransformStream.Position);
                lazyTransformStream.Seek(0, SeekOrigin.Begin);
                Assert.Equal(0, lazyTransformStream.Position);
                lazyTransformStream.Seek(-1, SeekOrigin.End);
                Assert.Equal(lazyTransformStream.Length - 1, lazyTransformStream.Position);
            }
        }

        [Fact]
        public void GivenReverseStreamFunction_WhenUsingLazyTransformStream_StreamIsReversed()
        {
            using (var lazyTransform = new LazyTransformReadOnlyStream<byte[]>(TestData, ReverseByteArray))
            {
                Assert.Equal(TestData.Length, lazyTransform.Length);

                var resultBuffer = new byte[TestData.Length];
                Assert.Equal(TestData.Length, lazyTransform.Read(resultBuffer, 0, TestData.Length));

                for (var i = 0; i < TestData.Length; i++)
                {
                    Assert.Equal(TestData[i], resultBuffer[TestData.Length - 1 - i]);
                }
            }
        }

        [Fact]
        public void GivenLazyTransformStream_WhenTransformingAnInputStream_InputStreamIsNotReadUntilLazyStreamIsRead()
        {
            using (var inputStream = new MemoryStream(TestData))
            using (var lazyTransform = new LazyTransformReadOnlyStream<Stream>(inputStream, ReadAndCreateNewStream))
            {
                Assert.Equal(0, inputStream.Position);
                Assert.Equal(TestData.Length, lazyTransform.Length);
                Assert.Equal(TestData.Length, inputStream.Position);
            }
        }

        [Fact]
        public void GivenLazyTransformStreamWithByteArray_WhenDisposing_IsDisposedCorrectly()
        {
            GCWatch gcWatch = GetGCWatch(TestData, ReverseByteArray);
            Assert.True(gcWatch.IsAlive);
        }

        [Fact]
        public void GivenLazyTransformStreamWithStream_WhenDisposing_IsDisposedCorrectly()
        {
            var inputStream = new MemoryStream(TestData);
            GCWatch gcWatch = GetGCWatch(inputStream, ReadAndCreateNewStream);
            inputStream.Dispose();
            inputStream = null;
            Assert.True(gcWatch.IsAlive);
        }

        [Fact]
        public void GivenLazyTransformStream_WhenTransformedDataHasDifferentLength_ReturnsTheCorrectStreamLength()
        {
            using (var lazyTransformStream = new LazyTransformReadOnlyStream<byte[]>(TestData, DoubleByteArray))
            {
                Assert.Equal(TestData.Length * 2, lazyTransformStream.Length);

                var readBuffer = new byte[lazyTransformStream.Length];
                lazyTransformStream.Read(readBuffer, 0, readBuffer.Length);
                Assert.Equal(readBuffer.Length, lazyTransformStream.Position);
            }
        }

        private static GCWatch GetGCWatch<T>(T inputData, Func<T, Stream> transformFunction)
        {
            var lazyTransform = new LazyTransformReadOnlyStream<T>(inputData, transformFunction);
            var result = new GCWatch(lazyTransform);

            // Read the entire stream
            var outputBuffer = new byte[lazyTransform.Length];
            lazyTransform.Read(outputBuffer, 0, outputBuffer.Length);

            return result;
        }

        private static Stream ReadAndCreateNewStream(Stream stream)
        {
            var resultBuffer = new byte[stream.Length];
            stream.Read(resultBuffer, 0, resultBuffer.Length);
            return new MemoryStream(resultBuffer);
        }

        private static Stream ReverseByteArray(byte[] input)
        {
            byte[] reversedInput = input.Reverse().ToArray();
            return new MemoryStream(reversedInput);
        }

        private static Stream DoubleByteArray(byte[] input)
        {
            byte[] result = new byte[input.Length * 2];
            for (var i = 0; i < input.Length; i++)
            {
                result[i * 2] = input[i];
                result[(i * 2) + 1] = input[i];
            }

            return new MemoryStream(result);
        }

        private class GCWatch
        {
            private readonly WeakReference _weakReference;

            public GCWatch(object value)
            {
                _weakReference = new WeakReference(value);
            }

            public bool IsAlive
            {
                get
                {
                    if (!_weakReference.IsAlive)
                    {
                        return true;
                    }

                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);

                    return !_weakReference.IsAlive;
                }
            }
        }
    }
}
