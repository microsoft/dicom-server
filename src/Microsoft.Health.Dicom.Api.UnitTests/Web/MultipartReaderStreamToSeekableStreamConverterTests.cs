// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Dicom.Api.Web;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Web
{
    public class MultipartReaderStreamToSeekableStreamConverterTests
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly MultipartReaderStreamToSeekableStreamConverter _seekableStreamConverter;

        public MultipartReaderStreamToSeekableStreamConverterTests()
        {
            _seekableStreamConverter = new MultipartReaderStreamToSeekableStreamConverter(Substitute.For<IHttpContextAccessor>());
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
            Stream nonseekableStream = SetupNonSeekableStreamException<IOException>();

            await Assert.ThrowsAsync<InvalidMultipartBodyPartException>(
                () => _seekableStreamConverter.ConvertAsync(nonseekableStream, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenANoneIOExceptionReadingStream_WhenConverted_ThenExceptionShouldBeRethrown()
        {
            Stream nonseekableStream = SetupNonSeekableStreamException<InvalidOperationException>();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _seekableStreamConverter.ConvertAsync(nonseekableStream, DefaultCancellationToken));
        }

        private Stream SetupNonSeekableStreamException<TException>()
            where TException : Exception, new()
        {
            Stream nonseekableStream = Substitute.For<Stream>();

            nonseekableStream.CanSeek.Returns(false);
            nonseekableStream.DrainAsync(DefaultCancellationToken)
                .Throws(_ => throw new TException());

            return nonseekableStream;
        }
    }
}
