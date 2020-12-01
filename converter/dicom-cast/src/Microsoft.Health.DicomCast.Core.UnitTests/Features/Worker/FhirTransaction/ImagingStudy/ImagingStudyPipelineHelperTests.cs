// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class ImagingStudyPipelineHelperTests
    {
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
    }
}
