// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.Store;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store
{
    public class DicomStoreHandlerTests
    {
        private const string DefaultContentType = "application/dicom";
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly IDicomInstanceEntryReaderManager _dicomInstanceEntryReaderManager = Substitute.For<IDicomInstanceEntryReaderManager>();
        private readonly IDicomStoreService _dicomStoreService = Substitute.For<IDicomStoreService>();
        private readonly DicomStoreHandler _dicomStoreHandler;

        public DicomStoreHandlerTests()
        {
            _dicomStoreHandler = new DicomStoreHandler(_dicomInstanceEntryReaderManager, _dicomStoreService);
        }

        [Fact]
        public async Task GivenNullRequestBody_WhenHandled_ThenDicomBadRequestExceptionShouldBeThrown()
        {
            DicomStoreRequest dicomStoreRequest = new DicomStoreRequest(null, DefaultContentType);

            await Assert.ThrowsAsync<DicomBadRequestException>(() => _dicomStoreHandler.Handle(dicomStoreRequest, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenInvalidStudyInstanceUid_WhenHandled_ThenDicomInvalidIdentifierExceptionShouldBeThrown()
        {
            DicomStoreRequest dicomStoreRequest = new DicomStoreRequest(Stream.Null, DefaultContentType, "invalid");

            await Assert.ThrowsAsync<DicomInvalidIdentifierException>(() => _dicomStoreHandler.Handle(dicomStoreRequest, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenUnsupportedContentType_WhenHandled_ThenUnsupportedMediaTypeExceptionShouldBeThrown()
        {
            _dicomInstanceEntryReaderManager.FindReader(default).ReturnsForAnyArgs((IDicomInstanceEntryReader)null);

            DicomStoreRequest dicomStoreRequest = new DicomStoreRequest(Stream.Null, "invalid");

            await Assert.ThrowsAsync<UnsupportedMediaTypeException>(() => _dicomStoreHandler.Handle(dicomStoreRequest, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenSupportedContentType_WhenHandled_ThenCorrectDicomStoreResponseShouldBeReturned()
        {
            const string studyInstanceUid = "1.2.3";

            IDicomInstanceEntry[] dicomInstanceEntries = new IDicomInstanceEntry[0];

            IDicomInstanceEntryReader dicomInstanceEntryReader = Substitute.For<IDicomInstanceEntryReader>();

            dicomInstanceEntryReader.ReadAsync(DefaultContentType, Stream.Null, DefaultCancellationToken).Returns(dicomInstanceEntries);

            _dicomInstanceEntryReaderManager.FindReader(DefaultContentType).Returns(dicomInstanceEntryReader);

            DicomStoreResponse dicomStoreResponse = new DicomStoreResponse(HttpStatusCode.OK);

            _dicomStoreService.ProcessAsync(dicomInstanceEntries, studyInstanceUid, DefaultCancellationToken).Returns(dicomStoreResponse);

            DicomStoreRequest dicomStoreRequest = new DicomStoreRequest(Stream.Null, DefaultContentType, studyInstanceUid);

            Assert.Equal(
                dicomStoreResponse,
                await _dicomStoreHandler.Handle(dicomStoreRequest, DefaultCancellationToken));
        }
    }
}
