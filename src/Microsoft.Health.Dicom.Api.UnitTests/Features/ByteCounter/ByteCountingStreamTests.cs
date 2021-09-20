// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Api.Features.ByteCounter;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.ByteCounter
{
    public class ByteCountingStreamTests
    {
        [Fact]
        public void BillingResponseLogMiddleware_ByteCountingStream_PropertiesCheck()
        {
            var memoryStream = new MemoryStream();
            using (var byteCountingStream = new ByteCountingStream(memoryStream))
            {
                byteCountingStream.Write(new byte[123]);
                Assert.Equal(memoryStream.CanRead, byteCountingStream.CanRead);
                Assert.Equal(memoryStream.CanSeek, byteCountingStream.CanSeek);
                Assert.Equal(memoryStream.CanWrite, byteCountingStream.CanWrite);
                Assert.Equal(memoryStream.CanTimeout, byteCountingStream.CanTimeout);
                Assert.Equal(memoryStream.Length, byteCountingStream.Length);
                Assert.Equal(memoryStream.Position, byteCountingStream.Position);
            }
        }

        [Fact]
        public void BillingResponseLogMiddleware_ByteCountingStream_ReadWrite()
        {
            var memoryStream = new MemoryStream();
            using (var byteCountingStream = new ByteCountingStream(memoryStream))
            {
                byte[] bytes = Encoding.UTF8.GetBytes("This is a string");
                byteCountingStream.Write(bytes);
                Assert.Equal(bytes.Length, byteCountingStream.WrittenByteCount);
                Assert.Equal(bytes.Length, memoryStream.Length);

                byteCountingStream.Flush();
                byteCountingStream.Seek(0, 0);
                int bytesRead = byteCountingStream.Read(new byte[bytes.Length], 0, bytes.Length);
                Assert.Equal(bytes.Length, bytesRead);
            }
        }

        [Fact]
        public async Task BillingResponseLogMiddleware_ByteCountingStream_WriteAsync()
        {
            byte[] bytes = Encoding.UTF8.GetBytes("This is a string");
            using (var byteCountingStream = new ByteCountingStream(new MemoryStream()))
            {
                await byteCountingStream.WriteAsync(bytes);
                Assert.Equal(bytes.Length, byteCountingStream.WrittenByteCount);
            }

            using (var byteCountingStream = new ByteCountingStream(new MemoryStream()))
            {
                await byteCountingStream.WriteAsync(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, byteCountingStream.WrittenByteCount);
            }

            using (var byteCountingStream = new ByteCountingStream(new MemoryStream()))
            {
                await byteCountingStream.WriteAsync(bytes.AsMemory(0, bytes.Length), CancellationToken.None);
                Assert.Equal(bytes.Length, byteCountingStream.WrittenByteCount);
            }

            using (var byteCountingStream = new ByteCountingStream(new MemoryStream()))
            {
                IAsyncResult asyncResult = byteCountingStream.BeginWrite(bytes, 0, bytes.Length, null, new object());
                Assert.Equal(bytes.Length, byteCountingStream.WrittenByteCount);
            }
        }

        [Fact]
        public void BillingResponseLogMiddleware_ByteCountingStream_WriteByte()
        {
            using (var byteCountingStream = new ByteCountingStream(new MemoryStream()))
            {
                byteCountingStream.WriteByte(0);
                Assert.Equal(1, byteCountingStream.WrittenByteCount);
                Assert.Equal(1, byteCountingStream.Length);
            }
        }

        [Fact]
        public void BillingResponseLogMiddleware_ByteCountingStream_WriteVaryingCounts()
        {
            byte[] bytes = Encoding.UTF8.GetBytes("This is a string");

            using (var byteCountingStream = new ByteCountingStream(new MemoryStream()))
            {
                // Specified count is the same as the byte array length
                byteCountingStream.Write(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, byteCountingStream.WrittenByteCount);
                Assert.Equal(bytes.Length, byteCountingStream.Length);
            }

            using (var byteCountingStream = new ByteCountingStream(new MemoryStream()))
            {
                // Specified count is less than the byte array length
                int count = bytes.Length / 2;
                byteCountingStream.Write(bytes, 0, count);
                Assert.Equal(count, byteCountingStream.WrittenByteCount);
                Assert.Equal(count, byteCountingStream.Length);
            }
        }

        [Fact]
        public void BillingResponseLogMiddleware_ByteCountingStream_NullBaseStreamThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new ByteCountingStream(null));
        }

        [Fact]
        public void BillingResponseLogMiddleware_ByteCountingStream_IsDisposed()
        {
            var memoryStream = new MemoryStream();
            var byteCountingStream = new ByteCountingStream(memoryStream);
            byte[] bytes = Encoding.UTF8.GetBytes("This is a string");
            byteCountingStream.Write(bytes);

            byteCountingStream.Dispose();
            Assert.Throws<ObjectDisposedException>(() => { byteCountingStream.WriteByte(1); });
            Assert.Throws<ObjectDisposedException>(() => { memoryStream.WriteByte(1); });
        }

        private async Task WriteToStream(Stream stream, byte[] bytes)
        {
            await stream.WriteAsync(bytes);
        }
    }
}
