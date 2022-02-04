// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using FellowOakDicom;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using NSubstitute;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class ImagingStudyPipelineHelperTests
    {
        private const string DefaultStudyInstanceUid = "111";
        private const string DefaultSeriesInstanceUid = "222";
        private const string DefaultSopInstanceUid = "333";
        private const string DefaultPatientResourceId = "555";

        private readonly IExceptionStore _exceptionStore = Substitute.For<IExceptionStore>();

        [Fact]
        public void GivenAChangeFeedEntryWithInvalidUtcTimeOffset_WhenDateTimeOffsetIsCalculated_ThenInvalidDicomTagValueExceptionIsThrown()
        {
            FhirTransactionContext fhirTransactionContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext();
            fhirTransactionContext.ChangeFeedEntry.Metadata.Add(DicomTag.TimezoneOffsetFromUTC, "0");

            Assert.Throws<InvalidDicomTagValueException>(
                () => ImagingStudyPipelineHelper.SetDateTimeOffSet(fhirTransactionContext));
        }

        [Theory]
        [InlineData(14, 0, "+1400")]
        [InlineData(-8, 0, "-0800")]
        [InlineData(-14, 0, "-1400")]
        [InlineData(8, 0, "+0800")]
        [InlineData(0, 0, "+0000")]
        [InlineData(8, 30, "+0830")]
        public void GivenAChangeFeedEntry_WhenDateTimeOffsetIsCalculated_ThenDateTimeOffsetIsSet(int hour, int minute, string dicomValue)
        {
            FhirTransactionContext fhirTransactionContext = FhirTransactionContextBuilder.DefaultFhirTransactionContext();

            DateTimeOffset utcTimeZoneOffset = new DateTimeOffset(2020, 1, 1, 0, 0, 0, new TimeSpan(hour, minute, 0));

            fhirTransactionContext.ChangeFeedEntry.Metadata.Add(DicomTag.TimezoneOffsetFromUTC, dicomValue);

            ImagingStudyPipelineHelper.SetDateTimeOffSet(fhirTransactionContext);

            Assert.Equal(utcTimeZoneOffset.Offset, fhirTransactionContext.UtcDateTimeOffset);
        }

        [Fact]
        public void GivenAccessionNumber_WhenGetAccessionNumber_ThenReturnsCorrectIdentifier()
        {
            string accessionNumber = "01234";
            var result = ImagingStudyPipelineHelper.GetAccessionNumber(accessionNumber);
            ValidationUtility.ValidateAccessionNumber(null, accessionNumber, result);
        }

        [Fact]
        public async Task SyncPropertiesAsync_PartialValidationNotEnabled_ThrowsError()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            Action<ImagingStudy, FhirTransactionContext> actionSubstitute = Substitute.For<Action<ImagingStudy, FhirTransactionContext>>();
            actionSubstitute.When(x => x.Invoke(imagingStudy, context)).Do(x => throw new InvalidDicomTagValueException("invalid tag", "invalid tag"));

            await Assert.ThrowsAsync<InvalidDicomTagValueException>(() => ImagingStudyPipelineHelper.SynchronizePropertiesAsync(imagingStudy, context, actionSubstitute, false, true, _exceptionStore));
        }

        [Fact]
        public async Task SyncPropertiesAsync_PartialValidationEnabledAndPropertyRequired_ThrowsError()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            Action<ImagingStudy, FhirTransactionContext> actionSubstitute = Substitute.For<Action<ImagingStudy, FhirTransactionContext>>();
            actionSubstitute.When(x => x.Invoke(imagingStudy, context)).Do(x => throw new InvalidDicomTagValueException("invalid tag", "invalid tag"));

            await Assert.ThrowsAsync<InvalidDicomTagValueException>(() => ImagingStudyPipelineHelper.SynchronizePropertiesAsync(imagingStudy, context, actionSubstitute, true, true, _exceptionStore));
        }

        [Fact]
        public async Task SyncPropertiesAsync_PartialValidationEnabledAndPropertyNotRequired_NoError()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            Action<ImagingStudy, FhirTransactionContext> actionSubstitute = Substitute.For<Action<ImagingStudy, FhirTransactionContext>>();
            actionSubstitute.When(x => x.Invoke(imagingStudy, context)).Do(x => throw new InvalidDicomTagValueException("invalid tag", "invalid tag"));

            await ImagingStudyPipelineHelper.SynchronizePropertiesAsync(imagingStudy, context, actionSubstitute, false, false, _exceptionStore);
        }
    }
}
