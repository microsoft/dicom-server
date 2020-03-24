// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions
{
    public class DicomDatasetExtensionsTests
    {
        [Fact]
        public void GivenDicomTagDoesNotExist_WhenGetSingleOrDefaultIsCalled_ThenDefaultValueShouldBeReturned()
        {
            var dataset = new DicomDataset();

            Assert.Equal(default, dataset.GetSingleValueOrDefault<string>(DicomTag.StudyInstanceUID));
            Assert.Equal(default, dataset.GetSingleValueOrDefault<DateTime>(DicomTag.AcquisitionDateTime));
            Assert.Equal(default, dataset.GetSingleValueOrDefault<short>(DicomTag.WarningReason));
        }

        [Fact]
        public void GivenDicomTagExists_WhenGetSingleOrDefaultIsCalled_ThenCorrectValueShouldBeReturned()
        {
            const string expectedValue = "IA";

            var dataset = new DicomDataset();

            dataset.Add(DicomTag.InstanceAvailability, expectedValue);

            Assert.Equal(expectedValue, dataset.GetSingleValueOrDefault<string>(DicomTag.InstanceAvailability));
        }

        [Fact]
        public void GivenNoDicomDateValue_WhenGetStringDateAsDateTimeIsCalled_ThenNullShouldBeReturned()
        {
            var dataset = new DicomDataset();

            Assert.Null(dataset.GetStringDateAsDateTime(DicomTag.StudyDate));
        }

        [Fact]
        public void GivenAValidaDicomDateValue_WhenGetStringDateAsDateTimeIsCalled_ThenCorrectDateTimeShouldBeReturned()
        {
            var dataset = new DicomDataset();

            dataset.Add(DicomTag.StudyDate, "20200301");

            Assert.Equal(
                new DateTime(2020, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                dataset.GetStringDateAsDateTime(DicomTag.StudyDate));
        }

        [Fact]
        public void GivenAnInvalidDicomDateValue_WhenGetStringDateAsDateTimeIsCalled_ThenNullShouldBeReturned()
        {
            var dataset = new DicomDataset();

            dataset.Add(DicomTag.StudyDate, "2010");

            Assert.Null(dataset.GetStringDateAsDateTime(DicomTag.StudyDate));
        }
    }
}
