// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Messages.Store;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store
{
    public class DicomStoreResponseBuilderTests
    {
        private readonly IUrlResolver _urlResolver = new MockUrlResolver();
        private readonly DicomStoreResponseBuilder _dicomStoreResponseBuilder;

        private readonly DicomDataset _dicomDataset1 = Samples.CreateRandomInstanceDataset(
            studyInstanceUid: "1",
            seriesInstanceUid: "2",
            sopInstanceUid: "3",
            sopClassUid: "4");

        private readonly DicomDataset _dicomDataset2 = Samples.CreateRandomInstanceDataset(
            studyInstanceUid: "10",
            seriesInstanceUid: "11",
            sopInstanceUid: "12",
            sopClassUid: "13");

        public DicomStoreResponseBuilderTests()
        {
            _dicomStoreResponseBuilder = new DicomStoreResponseBuilder(_urlResolver);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("1.2.3")]
        public void GivenNoEntries_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned(string studyInstanceUid)
        {
            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse(studyInstanceUid);

            Assert.NotNull(response);
            Assert.Equal(DicomStoreResponseStatus.None, response.Status);
            Assert.Null(response.Dataset);
        }

        [Fact]
        public void GivenOnlySuccessEntry_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
        {
            _dicomStoreResponseBuilder.AddSuccess(_dicomDataset1);

            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse(null);

            Assert.NotNull(response);
            Assert.Equal(DicomStoreResponseStatus.Success, response.Status);
            Assert.NotNull(response.Dataset);
            Assert.Single(response.Dataset);

            ValidationHelpers.ValidateReferencedSopSequence(
                response.Dataset,
                ("3", "/1/2/3", "4"));
        }

        [Fact]
        public void GivenOnlyFailedEntry_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
        {
            const ushort failureReasonCode = 100;

            _dicomStoreResponseBuilder.AddFailure(_dicomDataset2, failureReasonCode);

            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse(null);

            Assert.NotNull(response);
            Assert.Equal(DicomStoreResponseStatus.Failure, response.Status);
            Assert.NotNull(response.Dataset);
            Assert.Single(response.Dataset);

            ValidationHelpers.ValidateFailedSopSequence(
                response.Dataset,
                ("12", "13", failureReasonCode));
        }

        [Fact]
        public void GivenBothSuccessAndFailedEntires_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
        {
            _dicomStoreResponseBuilder.AddFailure(_dicomDataset1, TestConstants.ProcessingFailureReasonCode);
            _dicomStoreResponseBuilder.AddSuccess(_dicomDataset2);

            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse(null);

            Assert.NotNull(response);
            Assert.Equal(DicomStoreResponseStatus.PartialSuccess, response.Status);
            Assert.NotNull(response.Dataset);
            Assert.Equal(2, response.Dataset.Count());

            ValidationHelpers.ValidateFailedSopSequence(
                response.Dataset,
                ("3", "4", TestConstants.ProcessingFailureReasonCode));

            ValidationHelpers.ValidateReferencedSopSequence(
                response.Dataset,
                ("12", "/10/11/12", "13"));
        }

        [Fact]
        public void GivenMultipleSuccessAndFailedEntires_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
        {
            ushort failureReasonCode1 = TestConstants.ProcessingFailureReasonCode;
            ushort failureReasonCode2 = 100;

            _dicomStoreResponseBuilder.AddFailure(_dicomDataset1, failureReasonCode1);
            _dicomStoreResponseBuilder.AddFailure(_dicomDataset2, failureReasonCode2);

            _dicomStoreResponseBuilder.AddSuccess(_dicomDataset2);
            _dicomStoreResponseBuilder.AddSuccess(_dicomDataset1);

            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse(null);

            Assert.NotNull(response);
            Assert.Equal(DicomStoreResponseStatus.PartialSuccess, response.Status);
            Assert.NotNull(response.Dataset);
            Assert.Equal(2, response.Dataset.Count());

            ValidationHelpers.ValidateFailedSopSequence(
                response.Dataset,
                ("3", "4", failureReasonCode1),
                ("12", "13", failureReasonCode2));

            ValidationHelpers.ValidateReferencedSopSequence(
                response.Dataset,
                ("12", "/10/11/12", "13"),
                ("3", "/1/2/3", "4"));
        }

        [Fact]
        public void GivenNullDicomDatasetWhenAddingFailure_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
        {
            const ushort failureReasonCode = 300;

            _dicomStoreResponseBuilder.AddFailure(dicomDataset: null, failureReasonCode: failureReasonCode);

            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse(null);

            Assert.NotNull(response);
            Assert.Equal(DicomStoreResponseStatus.Failure, response.Status);
            Assert.NotNull(response.Dataset);
            Assert.Single(response.Dataset);

            ValidationHelpers.ValidateFailedSopSequence(
                response.Dataset,
                (null, null, failureReasonCode));
        }

        [Fact]
        public void GivenStudyInstanceUidAndThereIsOnlySuccessEntries_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
        {
            _dicomStoreResponseBuilder.AddSuccess(_dicomDataset1);

            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse("1");

            Assert.NotNull(response);
            Assert.NotNull(response.Dataset);

            // We have 2 items: RetrieveURL and ReferencedSOPSequence.
            Assert.Equal(2, response.Dataset.Count());
            Assert.Equal("1", response.Dataset.GetSingleValueOrDefault<string>(DicomTag.RetrieveURL));
        }

        [Fact]
        public void GivenStudyInstanceUidAndThereIsOnlyFailedEntries_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
        {
            _dicomStoreResponseBuilder.AddFailure(dicomDataset: null, failureReasonCode: 500);

            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse("1");

            Assert.NotNull(response);
            Assert.NotNull(response.Dataset);

            // We have 1 item: FailedSOPSequence.
            Assert.Single(response.Dataset);
        }

        [Fact]
        public void GivenStudyInstanceUidAndThereAreSuccessAndFailureEntries_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
        {
            _dicomStoreResponseBuilder.AddSuccess(_dicomDataset1);
            _dicomStoreResponseBuilder.AddFailure(_dicomDataset2, failureReasonCode: 200);

            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse("1");

            Assert.NotNull(response);
            Assert.NotNull(response.Dataset);

            // We have 3 items: RetrieveURL, FailedSOPSequence, and ReferencedSOPSequence.
            Assert.Equal(3, response.Dataset.Count());
            Assert.Equal("1", response.Dataset.GetSingleValueOrDefault<string>(DicomTag.RetrieveURL));
        }
    }
}
