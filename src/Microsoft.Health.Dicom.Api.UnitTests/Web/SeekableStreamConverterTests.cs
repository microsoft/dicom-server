// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Web;
using Microsoft.Health.Dicom.Core.Configs;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Web
{
    public class SeekableStreamConverterTests
    {
        private readonly SeekableStreamConverter _seekableStreamConverter;

        public SeekableStreamConverterTests()
        {
            IOptions<StoreConfiguration> configuration = Substitute.For<IOptions<StoreConfiguration>>();
            configuration.Value.Returns(new StoreConfiguration
            {
                MaxAllowedDicomFileSize = 1000000,
            });
            _seekableStreamConverter = new SeekableStreamConverter(Substitute.For<IHttpContextAccessor>(), configuration);
        }

        [Fact]
        public async Task GivenANonSeekableStream_WhenConverted_ThenANewSeekableStreamShouldBeReturned()
        {
            Stream nonseekableStream = Substitute.For<Stream>();

            nonseekableStream.CanSeek.Returns(false);

            Stream seekableStream = await _seekableStreamConverter.ConvertAsync(nonseekableStream, CancellationToken.None);

            Assert.NotNull(seekableStream);
            Assert.True(seekableStream.CanSeek);
        }

        [Fact]
        public async Task GivenAnIOExceptionReadingStream_WhenConverted_ThenInvalidMultipartBodyPartExceptionShouldBeThrown()
        {
            Stream nonseekableStream = SetupNonSeekableStreamException<IOException>();

            await Assert.ThrowsAsync<InvalidMultipartBodyPartException>(
                () => _seekableStreamConverter.ConvertAsync(nonseekableStream, CancellationToken.None));
        }

        [Fact]
        public async Task GivenANoneIOExceptionReadingStream_WhenConverted_ThenExceptionShouldBeRethrown()
        {
            Stream nonseekableStream = SetupNonSeekableStreamException<InvalidOperationException>();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _seekableStreamConverter.ConvertAsync(nonseekableStream, CancellationToken.None));
        }

        private Stream SetupNonSeekableStreamException<TException>()
            where TException : Exception, new()
        {
            Stream nonseekableStream = Substitute.For<Stream>();

            nonseekableStream.CanSeek.Returns(false);
            nonseekableStream.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>()).ThrowsForAnyArgs<TException>();

            return nonseekableStream;
        }
    }
}
