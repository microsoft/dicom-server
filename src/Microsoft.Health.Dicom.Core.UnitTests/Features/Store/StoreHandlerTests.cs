// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Core.Exceptions;
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
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly IInstanceEntryReaderManager _instanceEntryReaderManager = Substitute.For<IInstanceEntryReaderManager>();
        private readonly IStoreService _storeService = Substitute.For<IStoreService>();
        private readonly StoreHandler _storeHandler;

        public StoreHandlerTests()
        {
            _storeHandler = new StoreHandler(_instanceEntryReaderManager, _storeService);
        }

        [Fact]
        public async Task GivenNullRequestBody_WhenHandled_ThenBadRequestExceptionShouldBeThrown()
        {
            StoreRequest storeRequest = new StoreRequest(null, DefaultContentType);

            await Assert.ThrowsAsync<BadRequestException>(() => _storeHandler.Handle(storeRequest, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenInvalidStudyInstanceUid_WhenHandled_ThenInvalidIdentifierExceptionShouldBeThrown()
        {
            StoreRequest storeRequest = new StoreRequest(Stream.Null, DefaultContentType, "invalid");

            await Assert.ThrowsAsync<InvalidIdentifierException>(() => _storeHandler.Handle(storeRequest, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenUnsupportedContentType_WhenHandled_ThenUnsupportedMediaTypeExceptionShouldBeThrown()
        {
            _instanceEntryReaderManager.FindReader(default).ReturnsForAnyArgs((IInstanceEntryReader)null);

            StoreRequest storeRequest = new StoreRequest(Stream.Null, "invalid");

            await Assert.ThrowsAsync<UnsupportedMediaTypeException>(() => _storeHandler.Handle(storeRequest, DefaultCancellationToken));
        }

        [Fact]
        public async Task GivenSupportedContentType_WhenHandled_ThenCorrectStoreResponseShouldBeReturned()
        {
            const string studyInstanceUid = "1.2.3";

            IInstanceEntry[] dicomInstanceEntries = new IInstanceEntry[0];

            IInstanceEntryReader instanceEntryReader = Substitute.For<IInstanceEntryReader>();

            instanceEntryReader.ReadAsync(DefaultContentType, Stream.Null, DefaultCancellationToken).Returns(dicomInstanceEntries);

            _instanceEntryReaderManager.FindReader(DefaultContentType).Returns(instanceEntryReader);

            StoreResponse storeResponse = new StoreResponse(StoreResponseStatus.Success, new DicomDataset());

            _storeService.ProcessAsync(dicomInstanceEntries, studyInstanceUid, DefaultCancellationToken).Returns(storeResponse);

            StoreRequest storeRequest = new StoreRequest(Stream.Null, DefaultContentType, studyInstanceUid);

            Assert.Equal(
                storeResponse,
                await _storeHandler.Handle(storeRequest, DefaultCancellationToken));
        }
    }
}
