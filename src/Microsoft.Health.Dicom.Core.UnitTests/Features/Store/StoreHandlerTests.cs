// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Exceptions.Validation;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.Store;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store
{
    public class StoreHandlerTests
    {
        private const string DefaultContentType = "application/dicom";

        private readonly IDicomInstanceEntryReaderManager _dicomInstanceEntryReaderManager = Substitute.For<IDicomInstanceEntryReaderManager>();
        private readonly IStoreService _storeService = Substitute.For<IStoreService>();
        private readonly StoreHandler _storeHandler;

        public StoreHandlerTests()
        {
            _storeHandler = new StoreHandler(new DisabledAuthorizationService<DataActions>(), _dicomInstanceEntryReaderManager, _storeService);
        }

        [Fact]
        public async Task GivenNullRequestBody_WhenHandled_ThenBadRequestExceptionShouldBeThrown()
        {
            var storeRequest = new StoreRequest(null, DefaultContentType);

            await Assert.ThrowsAsync<BadRequestException>(() => _storeHandler.Handle(storeRequest, CancellationToken.None));
        }

        [Fact]
        public async Task GivenInvalidStudyInstanceUid_WhenHandled_ThenInvalidIdentifierExceptionShouldBeThrown()
        {
            var storeRequest = new StoreRequest(Stream.Null, DefaultContentType, "invalid");

            await Assert.ThrowsAsync<UidIsInValidException>(() => _storeHandler.Handle(storeRequest, CancellationToken.None));
        }

        [Fact]
        public async Task GivenUnsupportedContentType_WhenHandled_ThenUnsupportedMediaTypeExceptionShouldBeThrown()
        {
            _dicomInstanceEntryReaderManager.FindReader(default).ReturnsForAnyArgs((IDicomInstanceEntryReader)null);

            var storeRequest = new StoreRequest(Stream.Null, "invalid");

            await Assert.ThrowsAsync<UnsupportedMediaTypeException>(() => _storeHandler.Handle(storeRequest, CancellationToken.None));
        }

        [Fact]
        public async Task GivenSupportedContentType_WhenHandled_ThenCorrectStoreResponseShouldBeReturned()
        {
            const string studyInstanceUid = "1.2.3";

            IDicomInstanceEntry[] dicomInstanceEntries = Array.Empty<IDicomInstanceEntry>();
            IDicomInstanceEntryReader dicomInstanceEntryReader = Substitute.For<IDicomInstanceEntryReader>();
            var storeResponse = new StoreResponse(StoreResponseStatus.Success, new DicomDataset());
            using var source = new CancellationTokenSource();

            dicomInstanceEntryReader.ReadAsync(DefaultContentType, Stream.Null, source.Token).Returns(dicomInstanceEntries);
            _dicomInstanceEntryReaderManager.FindReader(DefaultContentType).Returns(dicomInstanceEntryReader);
            _storeService.ProcessAsync(dicomInstanceEntries, studyInstanceUid, source.Token).Returns(storeResponse);

            var storeRequest = new StoreRequest(Stream.Null, DefaultContentType, studyInstanceUid);

            Assert.Equal(
                storeResponse,
                await _storeHandler.Handle(storeRequest, source.Token));
        }
    }
}
