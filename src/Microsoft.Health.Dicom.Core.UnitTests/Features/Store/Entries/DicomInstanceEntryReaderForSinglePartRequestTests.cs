// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Web;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store.Entries
{
    public class DicomInstanceEntryReaderForSinglePartRequestTests
    {
        private const string DefaultContentType = "application/dicom";
        private const string DefaultBodyPartContentType = "application/dicom";
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private ISeekableStreamConverter _seekableStreamConverter = Substitute.For<ISeekableStreamConverter>();
        private readonly DicomInstanceEntryReaderForSinglePartRequest _dicomInstanceEntryReader;

        private Stream _stream = new MemoryStream();

        public DicomInstanceEntryReaderForSinglePartRequestTests()
        {
            _dicomInstanceEntryReader = new DicomInstanceEntryReaderForSinglePartRequest(
                NullLogger<DicomInstanceEntryReaderForSinglePartRequest>.Instance,
                _seekableStreamConverter);
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
                DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenBodyPartWithValidContentType_WhenReading_ThenCorrectResultsShouldBeReturned()
        {
            _seekableStreamConverter.ConvertAsync(_stream).Returns(_stream);
            IReadOnlyCollection<IDicomInstanceEntry> results = await _dicomInstanceEntryReader.ReadAsync(
                DefaultContentType,
                _stream,
                DefaultCancellationToken);

            Assert.NotNull(results);
            Assert.Collection(
                results,
                async item =>
                {
                    Assert.IsType<StreamOriginatedDicomInstanceEntry>(item);
                    Assert.Same(_stream, await item.GetStreamAsync(DefaultCancellationToken));
                });
        }
    }
}
