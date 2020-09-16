// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Web;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store.Entries
{
    public class DicomInstanceEntryReaderForApplicactionDicomRequestTests
    {
        private const string DefaultContentType = "application/dicom";
        private const string DefaultBodyPartContentType = "application/dicom";
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly ISeekableStreamConverter _seekableStreamConverter = Substitute.For<ISeekableStreamConverter>();
        private readonly DicomInstanceEntryReaderForApplicationDicomRequest _dicomInstanceEntryReader;

        private Stream _stream = new MemoryStream();

        public DicomInstanceEntryReaderForApplicactionDicomRequestTests()
        {
            _dicomInstanceEntryReader = new DicomInstanceEntryReaderForApplicationDicomRequest(
                NullLogger<DicomInstanceEntryReaderForMultipartRequest>.Instance,
                _seekableStreamConverter);
        }

        [Fact]
        public void GivenAnInvalidContentType_WhenCanReadIsCalledForIndividualRequest_ThenFalseShouldBeReturned()
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

        /*
        [Fact]
        public void GivenBodyPartWithInvalidContentType_WhenReading_ThenUnsupportedMediaTypeExceptionShouldBeThrown()
        {
            IMultipartReader multipartReader = SetupMultipartReader(
                _ => new MultipartBodyPart("application/dicom+json", _stream),
                _ => null);

            Assert.ThrowsAsync<UnsupportedMediaTypeException>(() => _dicomInstanceEntryReader.ReadAsync(DefaultContentType, _stream, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenBodyPartWithValidContentType_WhenReading_ThenCorrectResultsShouldBeReturned()
        {
            IMultipartReader multipartReader = SetupMultipartReader(
                _ => new MultipartBodyPart(DefaultBodyPartContentType, _stream),
                _ => null);

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

        [Fact]
        public async Task GivenAnException_WhenReading_ThenAlreadyProcessedStreamsShouldBeDisposed()
        {
            IMultipartReader multipartReader = SetupMultipartReader(
                _ => new MultipartBodyPart(DefaultBodyPartContentType, _stream),
                _ => throw new Exception());

            await Assert.ThrowsAsync<Exception>(() => _dicomInstanceEntryReader.ReadAsync(
                DefaultContentType,
                _stream,
                DefaultCancellationToken));

            Assert.Throws<ObjectDisposedException>(() => _stream.ReadByte());
        }

        [Fact]
        public async Task GivenAnExceptionWhileDisposing_WhenReading_ThenItShouldNotInterfereWithDisposingOtherInstances()
        {
            var streamToBeDisposed = new MemoryStream();

            IMultipartReader multipartReader = SetupMultipartReader(
                _ => new MultipartBodyPart(DefaultBodyPartContentType, streamToBeDisposed),
                _ =>
                {
                    // Dispose the previous stream so that when the code cleans up the resource, it throws exception.
                    streamToBeDisposed.Dispose();

                    return new MultipartBodyPart(DefaultBodyPartContentType, _stream);
                },
                _ => throw new Exception());

            await Assert.ThrowsAsync<Exception>(() => _dicomInstanceEntryReader.ReadAsync(
                DefaultContentType,
                _stream,
                DefaultCancellationToken));

            Assert.Throws<ObjectDisposedException>(() => _stream.ReadByte());
        }*/
    }
}
