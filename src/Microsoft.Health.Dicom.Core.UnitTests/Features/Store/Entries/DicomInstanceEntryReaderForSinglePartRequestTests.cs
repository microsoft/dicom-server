// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Web;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store.Entries
{
    public class DicomInstanceEntryReaderForSinglePartRequestTests
    {
        private const string DefaultContentType = "application/dicom";

        private readonly ISeekableStreamConverter _seekableStreamConverter = new TestSeekableStreamConverter();
        private readonly DicomInstanceEntryReaderForSinglePartRequest _dicomInstanceEntryReader;
        private readonly Stream _stream = new MemoryStream();

        public DicomInstanceEntryReaderForSinglePartRequestTests()
        {
            _dicomInstanceEntryReader = new DicomInstanceEntryReaderForSinglePartRequest(_seekableStreamConverter, CreateStoreConfiguration(1000000));
        }

        [Fact]
        public void GivenAnInvalidContentType_WhenCanReadIsCalledForSinglePartRequest_ThenFalseShouldBeReturned()
        {
            bool result = _dicomInstanceEntryReader.CanRead("dummy");

            Assert.False(result);
        }

        [Fact]
        public void GivenAnNonApplicationDicomContentType_WhenCanReadIsCalled_ThenFalseShouldBeReturned()
        {
            bool result = _dicomInstanceEntryReader.CanRead("multipart/related; boundary=123");

            Assert.False(result);
        }

        [Fact]
        public void GivenAnApplicattionDicomContentType_WhenCanReadIsCalled_ThenTrueShouldBeReturned()
        {
            bool result = _dicomInstanceEntryReader.CanRead(DefaultContentType);

            Assert.True(result);
        }

        [Fact]
        public async Task GivenUnSupportedContentType_WhenReading_ThenShouldThrowUnsupportedMediaTypeExceptionAsync()
        {
            await Assert.ThrowsAsync<UnsupportedMediaTypeException>(() => _dicomInstanceEntryReader.ReadAsync(
                "not/application/dicom",
                _stream,
                CancellationToken.None));
        }

        [Fact]
        public async Task GivenBodyPartWithValidContentType_WhenReading_ThenCorrectResultsShouldBeReturned()
        {
            using var source = new CancellationTokenSource();

            _stream.Write(Encoding.UTF8.GetBytes("someteststring"));

            IReadOnlyCollection<IDicomInstanceEntry> results = await _dicomInstanceEntryReader.ReadAsync(
                DefaultContentType,
                _stream,
                source.Token);

            Assert.NotNull(results);
            Assert.Collection(
                results,
                async item =>
                {
                    Assert.IsType<StreamOriginatedDicomInstanceEntry>(item);
                    Assert.Same(_stream, await item.GetStreamAsync(source.Token));
                });
        }

        [Fact]
        public async Task GivenBodyPartWithValidContentTypeExceedLimit_ThrowError()
        {
            var dicomInstanceEntryReaderLowLimit = new DicomInstanceEntryReaderForSinglePartRequest(_seekableStreamConverter, CreateStoreConfiguration(1));

            using var source = new CancellationTokenSource();

            Stream stream = new MemoryStream();
            stream.Write(Encoding.UTF8.GetBytes("someteststring"));
            stream.Seek(0, SeekOrigin.Begin);

            await Assert.ThrowsAsync<DicomFileLengthLimitExceededException>(
                () => dicomInstanceEntryReaderLowLimit.ReadAsync(
                    DefaultContentType,
                    stream,
                    source.Token));
        }

        [Fact]
        public async Task GivenBodyPartWithValidContentEqualsLimit_NoError()
        {
            var dicomInstanceEntryReaderLowLimit = new DicomInstanceEntryReaderForSinglePartRequest(_seekableStreamConverter, CreateStoreConfiguration(14));

            using var source = new CancellationTokenSource();

            Stream stream = new MemoryStream();
            stream.Write(Encoding.UTF8.GetBytes("someteststring"));
            stream.Seek(0, SeekOrigin.Begin);

            IReadOnlyCollection<IDicomInstanceEntry> results = await _dicomInstanceEntryReader.ReadAsync(
                DefaultContentType,
                _stream,
                source.Token);

            Assert.NotNull(results);
            Assert.Collection(
                results,
                async item =>
                {
                    Assert.IsType<StreamOriginatedDicomInstanceEntry>(item);
                    Assert.Same(_stream, await item.GetStreamAsync(source.Token));
                });
        }

        private IOptions<StoreConfiguration> CreateStoreConfiguration(long maxSize)
        {
            var configuration = Substitute.For<IOptions<StoreConfiguration>>();
            configuration.Value.Returns(new StoreConfiguration
            {
                MaxAllowedDicomFileSize = maxSize,
            });
            return configuration;
        }

        private class TestSeekableStreamConverter : ISeekableStreamConverter
        {
            public async Task<Stream> ConvertAsync(Stream stream, CancellationToken cancellationToken = default)
            {
                MemoryStream seekableStream = new MemoryStream();
                stream.CopyTo(seekableStream);

                await seekableStream.DrainAsync(cancellationToken);

                seekableStream.Seek(0, SeekOrigin.Begin);

                return seekableStream;
            }
        }
    }
}
