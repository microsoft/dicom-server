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
        private readonly DicomDataset _dicomDataset = new DicomDataset();

        [Fact]
        public void GivenDicomTagDoesNotExist_WhenGetSingleOrDefaultIsCalled_ThenDefaultValueShouldBeReturned()
        {
            Assert.Equal(default, _dicomDataset.GetSingleValueOrDefault<string>(DicomTag.StudyInstanceUID));
            Assert.Equal(default, _dicomDataset.GetSingleValueOrDefault<DateTime>(DicomTag.AcquisitionDateTime));
            Assert.Equal(default, _dicomDataset.GetSingleValueOrDefault<short>(DicomTag.WarningReason));
        }

        [Fact]
        public void GivenDicomTagExists_WhenGetSingleOrDefaultIsCalled_ThenCorrectValueShouldBeReturned()
        {
            const string expectedValue = "IA";

            _dicomDataset.Add(DicomTag.InstanceAvailability, expectedValue);

            Assert.Equal(expectedValue, _dicomDataset.GetSingleValueOrDefault<string>(DicomTag.InstanceAvailability));
        }

        [Fact]
        public void GivenNoDicomDateValue_WhenGetStringDateAsDateTimeIsCalled_ThenNullShouldBeReturned()
        {
            Assert.Null(_dicomDataset.GetStringDateAsDateTime(DicomTag.StudyDate));
        }

        [Fact]
        public void GivenAValidDicomDateValue_WhenGetStringDateAsDateTimeIsCalled_ThenCorrectDateTimeShouldBeReturned()
        {
            _dicomDataset.Add(DicomTag.StudyDate, "20200301");

            Assert.Equal(
                new DateTime(2020, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                _dicomDataset.GetStringDateAsDateTime(DicomTag.StudyDate));
        }

        [Fact]
        public void GivenAnInvalidDicomDateValue_WhenGetStringDateAsDateTimeIsCalled_ThenNullShouldBeReturned()
        {
            _dicomDataset.Add(DicomTag.StudyDate, "2010");

            Assert.Null(_dicomDataset.GetStringDateAsDateTime(DicomTag.StudyDate));
        }

        [Fact]
        public void GivenANullValue_WhenAddValueIfNotNullIsCalled_ThenValueShouldNotBeAdded()
        {
            DicomTag dicomTag = DicomTag.StudyInstanceUID;

            _dicomDataset.AddValueIfNotNull(dicomTag, (string)null);

            Assert.False(_dicomDataset.TryGetSingleValue(dicomTag, out string _));
        }

        [Fact]
        public void GivenANonNullValue_WhenAddValueIfNotNullIsCalled_ThenValueShouldBeAdded()
        {
            const string value = "123";

            DicomTag dicomTag = DicomTag.StudyInstanceUID;

            _dicomDataset.AddValueIfNotNull(dicomTag, value);

            Assert.True(_dicomDataset.TryGetSingleValue<string>(dicomTag, out string writtenValue));
            Assert.Equal(writtenValue, value);
        }
    }
}
