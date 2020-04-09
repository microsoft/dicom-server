// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Net;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Messages.Store;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store
{
    public class DicomStoreResponseBuilderTests
    {
        private readonly IUrlResolver _urlResolver = Substitute.For<IUrlResolver>();
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
            _urlResolver.ResolveRetrieveStudyUri(Arg.Any<string>()).Returns(x =>
            {
                var studyInstanceUid = x.ArgAt<string>(0);

                return new Uri(studyInstanceUid, UriKind.Relative);
            });

            _urlResolver.ResolveRetrieveInstanceUri(Arg.Any<DicomInstanceIdentifier>()).Returns(x =>
            {
                var dicomInstance = x.ArgAt<DicomInstanceIdentifier>(0);

                return new Uri(
                    $"/{dicomInstance.StudyInstanceUid}/{dicomInstance.SeriesInstanceUid}/{dicomInstance.SopInstanceUid}",
                    UriKind.Relative);
            });

            _dicomStoreResponseBuilder = new DicomStoreResponseBuilder(_urlResolver);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("1.2.3")]
        public void GivenNoEntries_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned(string studyInstanceUid)
        {
            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse(studyInstanceUid);

            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(response.Dataset);
        }

        [Fact]
        public void GivenOnlySuccessEntry_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
        {
            _dicomStoreResponseBuilder.AddSuccess(_dicomDataset1);

            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse(null);

            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Dataset);
            Assert.Single(response.Dataset);

            ValidateReferencedSopSequence(
                response.Dataset,
                ("3", "/1/2/3", "4"));
        }

        [Fact]
        public void GivenOnlyFailedEntry_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
        {
            _dicomStoreResponseBuilder.AddFailure(_dicomDataset2, 100);

            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse(null);

            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.Conflict, response.StatusCode);
            Assert.NotNull(response.Dataset);
            Assert.Single(response.Dataset);

            ValidateFailedSopSequence(
                response.Dataset,
                ("12", "13", 100));
        }

        [Fact]
        public void GivenBothSuccessAndFailedEntires_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
        {
            _dicomStoreResponseBuilder.AddFailure(_dicomDataset1);
            _dicomStoreResponseBuilder.AddSuccess(_dicomDataset2);

            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse(null);

            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.Accepted, response.StatusCode);
            Assert.NotNull(response.Dataset);
            Assert.Equal(2, response.Dataset.Count());

            ValidateFailedSopSequence(
                response.Dataset,
                ("3", "4", 272));

            ValidateReferencedSopSequence(
                response.Dataset,
                ("12", "/10/11/12", "13"));
        }

        [Fact]
        public void GivenMultipleSuccessAndFailedEntires_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
        {
            _dicomStoreResponseBuilder.AddFailure(_dicomDataset1);
            _dicomStoreResponseBuilder.AddFailure(_dicomDataset2, 100);

            _dicomStoreResponseBuilder.AddSuccess(_dicomDataset2);
            _dicomStoreResponseBuilder.AddSuccess(_dicomDataset1);

            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse(null);

            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.Accepted, response.StatusCode);
            Assert.NotNull(response.Dataset);
            Assert.Equal(2, response.Dataset.Count());

            ValidateFailedSopSequence(
                response.Dataset,
                ("3", "4", 272),
                ("12", "13", 100));

            ValidateReferencedSopSequence(
                response.Dataset,
                ("12", "/10/11/12", "13"),
                ("3", "/1/2/3", "4"));
        }

        [Fact]
        public void GivenNullDicomDatasetWhenAddingFailuire_WhenResponseIsBuilt_ThenCorrectResponseShouldBeReturned()
        {
            _dicomStoreResponseBuilder.AddFailure(failureReason: 300);

            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse(null);

            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.Conflict, response.StatusCode);
            Assert.NotNull(response.Dataset);
            Assert.Single(response.Dataset);

            ValidateFailedSopSequence(
                response.Dataset,
                (null, null, 300));
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
            _dicomStoreResponseBuilder.AddFailure(failureReason: 300);

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
            _dicomStoreResponseBuilder.AddFailure(_dicomDataset2);

            DicomStoreResponse response = _dicomStoreResponseBuilder.BuildResponse("1");

            Assert.NotNull(response);
            Assert.NotNull(response.Dataset);

            // We have 3 items: RetrieveURL, FailedSOPSequence, and ReferencedSOPSequence.
            Assert.Equal(3, response.Dataset.Count());
            Assert.Equal("1", response.Dataset.GetSingleValueOrDefault<string>(DicomTag.RetrieveURL));
        }

        private void ValidateReferencedSopSequence(DicomDataset actualDicomDataset, params (string SopInstanceUid, string RetrieveUri, string SopClassUid)[] expectedValues)
        {
            Assert.True(actualDicomDataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence sequence));
            Assert.Equal(expectedValues.Length, sequence.Count());

            for (int i = 0; i < expectedValues.Length; i++)
            {
                DicomDataset actual = sequence.ElementAt(i);

                Assert.Equal(expectedValues[i].SopInstanceUid, actual.GetSingleValueOrDefault<string>(DicomTag.ReferencedSOPInstanceUID));
                Assert.Equal(expectedValues[i].RetrieveUri, actual.GetSingleValueOrDefault<string>(DicomTag.RetrieveURL));
                Assert.Equal(expectedValues[i].SopClassUid, actual.GetSingleValueOrDefault<string>(DicomTag.ReferencedSOPClassUID));
            }
        }

        private void ValidateFailedSopSequence(DicomDataset actualDicomDataset, params (string SopInstanceUid, string SopClassUid, ushort FailureReason)[] expectedValues)
        {
            Assert.True(actualDicomDataset.TryGetSequence(DicomTag.FailedSOPSequence, out DicomSequence sequence));
            Assert.Equal(expectedValues.Length, sequence.Count());

            for (int i = 0; i < expectedValues.Length; i++)
            {
                DicomDataset actual = sequence.ElementAt(i);

                ValidateNullOrCorrectValue(expectedValues[i].SopInstanceUid, actual, DicomTag.ReferencedSOPInstanceUID);
                ValidateNullOrCorrectValue(expectedValues[i].SopClassUid, actual, DicomTag.ReferencedSOPClassUID);

                Assert.Equal(expectedValues[i].FailureReason, actual.GetSingleValueOrDefault<ushort>(DicomTag.FailureReason));
            }

            void ValidateNullOrCorrectValue(string expectedValue, DicomDataset actual, DicomTag dicomTag)
            {
                if (expectedValue == null)
                {
                    Assert.False(actual.TryGetSingleValue(dicomTag, out string _));
                }
                else
                {
                    Assert.Equal(expectedValue, actual.GetSingleValueOrDefault<string>(dicomTag));
                }
            }
        }
    }
}
