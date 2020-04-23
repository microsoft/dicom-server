// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Api.Web;
using Microsoft.IO;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Web
{
    public class MultipartReaderStreamToSeekableStreamConverterTests
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

        private readonly MultipartReaderStreamToSeekableStreamConverter _seekableStreamConverter;

        private int _numberOfDisposeCalled;

        public MultipartReaderStreamToSeekableStreamConverterTests()
        {
            _recyclableMemoryStreamManager.StreamDisposed += () =>
            {
                _numberOfDisposeCalled++;
            };

            _seekableStreamConverter = new MultipartReaderStreamToSeekableStreamConverter(_recyclableMemoryStreamManager);
        }

        [Fact]
        public async Task GivenANonSeekableStream_WhenConverted_ThenANewSeekableStreamShouldBeReturned()
        {
            Stream nonseekableStream = Substitute.For<Stream>();

            nonseekableStream.CanSeek.Returns(false);

            Stream seekableStream = await _seekableStreamConverter.ConvertAsync(nonseekableStream, DefaultCancellationToken);

            Assert.NotNull(seekableStream);
            Assert.True(seekableStream.CanSeek);
        }

        [Fact]
        public async Task GivenAnIOExceptionReadingStream_WhenConverted_ThenInvalidMultipartBodyPartExceptionShouldBeThrown()
        {
            Stream nonseekableStream = SetupNonSeeableStreamException<IOException>();

            await Assert.ThrowsAsync<InvalidMultipartBodyPartException>(
                () => _seekableStreamConverter.ConvertAsync(nonseekableStream, DefaultCancellationToken));

            Assert.Equal(1, _numberOfDisposeCalled);
        }

        [Fact]
        public async Task GivenANoneIOExceptionReadingStream_WhenConverted_ThenExceptionShouldBeRethrown()
        {
            Stream nonseekableStream = SetupNonSeeableStreamException<InvalidOperationException>();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _seekableStreamConverter.ConvertAsync(nonseekableStream, DefaultCancellationToken));

            Assert.Equal(1, _numberOfDisposeCalled);
        }

        private Stream SetupNonSeeableStreamException<TException>()
            where TException : Exception, new()
        {
            Stream nonseekableStream = Substitute.For<Stream>();

            nonseekableStream.CanSeek.Returns(false);
            nonseekableStream.When(stream => stream.CopyToAsync(Arg.Any<Stream>(), DefaultCancellationToken))
                .Do(_ => throw new TException());

            return nonseekableStream;
        }
    }
}
