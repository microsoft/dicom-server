// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Exceptions.Validation;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class RetrieveMetadataHandlerTests
    {
        private readonly IRetrieveMetadataService _retrieveMetadataService;
        private readonly RetrieveMetadataHandler _retrieveMetadataHandler;

        public RetrieveMetadataHandlerTests()
        {
            _retrieveMetadataService = Substitute.For<IRetrieveMetadataService>();
            _retrieveMetadataHandler = new RetrieveMetadataHandler(new DisabledAuthorizationService<DataActions>(), _retrieveMetadataService);
        }

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("345%^&")]
        [InlineData("()")]
        public async Task GivenARequestWithInvalidStudyInstanceIdentifier_WhenHandlerIsExecuted_ThenDicomInvalidIdentifierExceptionIsThrown(string studyInstanceUid)
        {
            EnsureArg.IsNotNull(studyInstanceUid, nameof(studyInstanceUid));
            string ifNoneMatch = null;
            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, ifNoneMatch);
            await Assert.ThrowsAsync<UidIsInValidException>(() => _retrieveMetadataHandler.Handle(request, CancellationToken.None));
        }

        [Theory]
        [InlineData("aaaa-bbbb", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("aaaa-bbbb", " ")]
        [InlineData("aaaa-bbbb", "345%^&")]
        [InlineData("aaaa-bbbb", "aaaa-bbbb")]
        public async Task GivenARequestWithInvalidStudyIdentifier_WhenRetrievingSeriesMetadata_ThenDicomInvalidIdentifierExceptionIsThrown(string studyInstanceUid, string seriesInstanceUid)
        {
            EnsureArg.IsNotNull(studyInstanceUid, nameof(studyInstanceUid));
            string ifNoneMatch = null;
            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, ifNoneMatch);
            await Assert.ThrowsAsync<UidIsInValidException>(() => _retrieveMetadataHandler.Handle(request, CancellationToken.None));
        }

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("345%^&")]
        [InlineData("aaaa-bbbb")]
        [InlineData("()")]
        public async Task GivenARequestWithInvalidSeriesIdentifier_WhenRetrievingSeriesMetadata_ThenDicomInvalidIdentifierExceptionIsThrown(string seriesInstanceUid)
        {
            EnsureArg.IsNotNull(seriesInstanceUid, nameof(seriesInstanceUid));
            string ifNoneMatch = null;
            RetrieveMetadataRequest request = new RetrieveMetadataRequest(TestUidGenerator.Generate(), seriesInstanceUid, ifNoneMatch);
            await Assert.ThrowsAsync<UidIsInValidException>(() => _retrieveMetadataHandler.Handle(request, CancellationToken.None));
        }

        [Theory]
        [InlineData("aaaa-bbbb1", "aaaa-bbbb2", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("aaaa-bbbb1", "aaaa-bbbb2", "345%^&")]
        [InlineData("aaaa-bbbb1", "aaaa-bbbb2", "aaaa-bbbb2")]
        [InlineData("aaaa-bbbb1", "aaaa-bbbb2", "aaaa-bbbb1")]
        public async Task GivenARequestWithInvalidStudyAndSeriesInstanceIdentifier_WhenRetrievingInstanceMetadata_ThenDicomInvalidIdentifierExceptionIsThrown(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            EnsureArg.IsNotNull(studyInstanceUid, nameof(studyInstanceUid));
            string ifNoneMatch = null;
            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch);
            await Assert.ThrowsAsync<UidIsInValidException>(() => _retrieveMetadataHandler.Handle(request, CancellationToken.None));
        }

        [Theory]
        [InlineData("aaaa-bbbb2", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("aaaa-bbbb2", "345%^&")]
        [InlineData("aaaa-bbbb2", "aaaa-bbbb2")]
        [InlineData("aaaa-bbbb2", " ")]
        public async Task GivenARequestWithInvalidSeriesInstanceIdentifier_WhenRetrievingInstanceMetadata_ThenDicomInvalidIdentifierExceptionIsThrown(string seriesInstanceUid, string sopInstanceUid)
        {
            EnsureArg.IsNotNull(seriesInstanceUid, nameof(seriesInstanceUid));
            string ifNoneMatch = null;
            RetrieveMetadataRequest request = new RetrieveMetadataRequest(TestUidGenerator.Generate(), seriesInstanceUid, sopInstanceUid, ifNoneMatch);
            await Assert.ThrowsAsync<UidIsInValidException>(() => _retrieveMetadataHandler.Handle(request, CancellationToken.None));
        }

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("345%^&")]
        [InlineData("aaaa-bbbb")]
        [InlineData("()")]
        public async Task GivenARequestWithInvalidSopInstanceIdentifier_WhenRetrievingInstanceMetadata_ThenDicomInvalidIdentifierExceptionIsThrown(string sopInstanceUid)
        {
            EnsureArg.IsNotNull(sopInstanceUid, nameof(sopInstanceUid));
            string ifNoneMatch = null;
            RetrieveMetadataRequest request = new RetrieveMetadataRequest(TestUidGenerator.Generate(), TestUidGenerator.Generate(), sopInstanceUid, ifNoneMatch);
            await Assert.ThrowsAsync<UidIsInValidException>(() => _retrieveMetadataHandler.Handle(request, CancellationToken.None));
        }

        [Theory]
        [InlineData("1", "1", "2")]
        [InlineData("1", "2", "1")]
        [InlineData("1", "2", "2")]
        public async Task GivenRepeatedIdentifiers_WhenRetrievingInstanceMetadata_ThenDicomBadRequestExceptionIsThrownAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            string ifNoneMatch = null;
            const string expectedErrorMessage = "The values for StudyInstanceUID, SeriesInstanceUID, SOPInstanceUID must be unique.";
            var request = new RetrieveMetadataRequest(
                studyInstanceUid: studyInstanceUid,
                seriesInstanceUid: seriesInstanceUid,
                sopInstanceUid: sopInstanceUid,
                ifNoneMatch: ifNoneMatch);

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _retrieveMetadataHandler.Handle(request, CancellationToken.None));

            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        [Fact]
        public async Task GivenARequestWithValidInstanceIdentifier_WhenRetrievingStudyInstanceMetadata_ThenResponseMetadataIsReturnedSuccessfully()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string ifNoneMatch = null;

            RetrieveMetadataResponse response = SetupRetrieveMetadataResponse();
            _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid).Returns(response);

            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, ifNoneMatch);
            await ValidateRetrieveMetadataResponse(response, request);
        }

        [Theory]
        [InlineData("2.25.282803907956301170169364749856339309473", "1-1")]
        public async Task GivenARequestWithValidInstanceIdentifierAndIfNoneMatchHeader_WhenRetrievingStudyInstanceMetadata_ThenNotModifiedResponseIsReturned(string studyInstanceUid, string ifNoneMatch)
        {
            RetrieveMetadataResponse response = SetupRetrieveMetadataResponseForValidatingCache(true, ifNoneMatch);
            _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, ifNoneMatch).Returns(response);

            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, ifNoneMatch);
            await ValidateRetrieveMetadataResponse(response, request);
        }

        [Theory]
        [InlineData("2.25.282803907956301170169364749856339309473", "1-1", "2-2")]
        public async Task GivenARequestWithValidInstanceIdentifierAndExpiredIfNoneMatchHeader_WhenRetrievingStudyInstanceMetadata_ThenResponseMetadataIsReturnedSuccessfully(string studyInstanceUid, string ifNoneMatch, string eTag)
        {
            RetrieveMetadataResponse response = SetupRetrieveMetadataResponseForValidatingCache(false, eTag);
            _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(studyInstanceUid, ifNoneMatch).Returns(response);

            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, ifNoneMatch);
            await ValidateRetrieveMetadataResponse(response, request);
        }

        [Fact]
        public async Task GivenARequestWithValidInstanceIdentifier_WhenRetrievingSeriesInstanceMetadata_ThenResponseMetadataIsReturnedSuccessfully()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string ifNoneMatch = null;

            RetrieveMetadataResponse response = SetupRetrieveMetadataResponse();
            _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid).Returns(response);

            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, ifNoneMatch);
            await ValidateRetrieveMetadataResponse(response, request);
        }

        [Theory]
        [InlineData("2.25.282803907956301170169364749856339309473", "2.25.73315770910160804467620423579356140698", "1-1")]
        public async Task GivenARequestWithValidInstanceIdentifierAndIfNoneMatchHeader_WhenRetrievingSeriesInstanceMetadata_ThenNotModifiedResponseIsReturned(string studyInstanceUid, string seriesInstanceUid, string ifNoneMatch)
        {
            RetrieveMetadataResponse response = SetupRetrieveMetadataResponseForValidatingCache(true, ifNoneMatch);
            _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, ifNoneMatch).Returns(response);

            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, ifNoneMatch);
            await ValidateRetrieveMetadataResponse(response, request);
        }

        [Theory]
        [InlineData("2.25.282803907956301170169364749856339309473", "2.25.73315770910160804467620423579356140698", "1-1", "2-2")]
        public async Task GivenARequestWithValidInstanceIdentifierAndExpiredIfNoneMatchHeader_WhenRetrievingSeriesInstanceMetadata_ThenResponseMetadataIsReturnedSuccessfully(string studyInstanceUid, string seriesInstanceUid, string ifNoneMatch, string eTag)
        {
            RetrieveMetadataResponse response = SetupRetrieveMetadataResponseForValidatingCache(false, eTag);
            _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, ifNoneMatch).Returns(response);

            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, ifNoneMatch);
            await ValidateRetrieveMetadataResponse(response, request);
        }

        [Fact]
        public async Task GivenARequestWithValidInstanceIdentifier_WhenRetrievingSopInstanceMetadata_ThenResponseMetadataIsReturnedSuccessfully()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            string ifNoneMatch = null;

            RetrieveMetadataResponse response = SetupRetrieveMetadataResponse();
            _retrieveMetadataService.RetrieveSopInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Returns(response);

            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch);
            await ValidateRetrieveMetadataResponse(response, request);
        }

        [Theory]
        [InlineData("2.25.282803907956301170169364749856339309473", "2.25.73315770910160804467620423579356140698", "2.25.224979845195287507011636517849022735847", "1-1")]
        public async Task GivenARequestWithValidInstanceIdentifierAndIfNoneMatchHeader_WhenRetrievingSopInstanceMetadata_ThenNotModifiedResponseIsReturned(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string ifNoneMatch)
        {
            RetrieveMetadataResponse response = SetupRetrieveMetadataResponseForValidatingCache(true, ifNoneMatch);
            _retrieveMetadataService.RetrieveSopInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch).Returns(response);

            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch);
            await ValidateRetrieveMetadataResponse(response, request);
        }

        [Theory]
        [InlineData("2.25.282803907956301170169364749856339309473", "2.25.73315770910160804467620423579356140698", "2.25.224979845195287507011636517849022735847", "1-1", "2-2")]
        public async Task GivenARequestWithValidInstanceIdentifierAndExpiredIfNoneMatchHeader_WhenRetrievingSopInstanceMetadata_ThenResponseMetadataIsReturnedSuccessfully(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string ifNoneMatch, string eTag)
        {
            RetrieveMetadataResponse response = SetupRetrieveMetadataResponseForValidatingCache(false, eTag);
            _retrieveMetadataService.RetrieveSopInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch).Returns(response);

            RetrieveMetadataRequest request = new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch);
            await ValidateRetrieveMetadataResponse(response, request);
        }

        private static RetrieveMetadataResponse SetupRetrieveMetadataResponse()
        {
            return new RetrieveMetadataResponse(
                new List<DicomDataset> { new DicomDataset() });
        }

        private static RetrieveMetadataResponse SetupRetrieveMetadataResponseForValidatingCache(bool isCacheValid, string eTag)
        {
            List<DicomDataset> responseMetadata = new List<DicomDataset>();

            if (!isCacheValid)
            {
                responseMetadata.Add(new DicomDataset());
            }

            return new RetrieveMetadataResponse(
                responseMetadata,
                isCacheValid: isCacheValid,
                eTag: eTag);
        }

        private async Task ValidateRetrieveMetadataResponse(RetrieveMetadataResponse response, RetrieveMetadataRequest request)
        {
            RetrieveMetadataResponse actualResponse = await _retrieveMetadataHandler.Handle(request, CancellationToken.None);
            Assert.Same(response, actualResponse);
        }
    }
}
