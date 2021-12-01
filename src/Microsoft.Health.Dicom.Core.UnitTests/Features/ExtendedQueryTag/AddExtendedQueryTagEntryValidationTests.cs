// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ExtendedQueryTag
{
    public class AddExtendedQueryTagEntryValidationTests
    {
        [Fact]
        public void GivenValidAddExtendedQueryTagEntry_WhenValidating_ShouldSucced()
        {
            AddExtendedQueryTagEntry addExtendedQueryTagEntry = new AddExtendedQueryTagEntry() { Path = "00101001", Level = QueryTagLevel.Study };
            var validationContext = new ValidationContext(addExtendedQueryTagEntry);
            IEnumerable<ValidationResult> results = addExtendedQueryTagEntry.Validate(validationContext);
            Assert.Empty(results);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GivenInvalidPath_WhenValidating_ResultShouldHaveExceptions(string pathValue)
        {
            AddExtendedQueryTagEntry addExtendedQueryTagEntry = new AddExtendedQueryTagEntry() { Path = pathValue, Level = QueryTagLevel.Study };
            var validationContext = new ValidationContext(addExtendedQueryTagEntry);
            IEnumerable<ValidationResult> results = addExtendedQueryTagEntry.Validate(validationContext);
            Assert.Single(results);
            Assert.Equal("The Dicom Tag Property Path must be specified and must not be null, empty or whitespace.", results.First().ErrorMessage);
        }

        [Fact]
        public void GivenEmptyNullOrWhitespaceLevel_WhenValidating_ResultShouldHaveExceptions()
        {
            AddExtendedQueryTagEntry addExtendedQueryTagEntry = new AddExtendedQueryTagEntry() { Path = "00101001", Level = null };
            var validationContext = new ValidationContext(addExtendedQueryTagEntry);
            IEnumerable<ValidationResult> results = addExtendedQueryTagEntry.Validate(validationContext);
            Assert.Collection(
                results,
                item => Assert.Equal("The Dicom Tag Property Level must be specified and must not be null, empty or whitespace.", item.ErrorMessage));
        }

        [Fact]
        public void GivenInvalidLevel_WhenValidating_ResultShouldHaveExceptions()
        {
            AddExtendedQueryTagEntry addExtendedQueryTagEntry = new AddExtendedQueryTagEntry() { Path = "00101001", Level = (QueryTagLevel)47 };
            var validationContext = new ValidationContext(addExtendedQueryTagEntry);
            IEnumerable<ValidationResult> results = addExtendedQueryTagEntry.Validate(validationContext);
            Assert.Single(results);
            Assert.Equal("Input Dicom Tag Level '47' is invalid. It must have value 'Study', 'Series' or 'Instance'.", results.First().ErrorMessage);
        }

        [Fact]
        public void GivenMultipleValidationErrors_WhenValidating_ResultShouldHaveExceptions()
        {
            AddExtendedQueryTagEntry addExtendedQueryTagEntry = new AddExtendedQueryTagEntry() { Path = "", Level = null };
            var validationContext = new ValidationContext(addExtendedQueryTagEntry);
            IEnumerable<ValidationResult> results = addExtendedQueryTagEntry.Validate(validationContext);
            Assert.Collection(
                results,
                item => Assert.Equal("The Dicom Tag Property Path must be specified and must not be null, empty or whitespace.", item.ErrorMessage),
                item => Assert.Equal("The Dicom Tag Property Level must be specified and must not be null, empty or whitespace.", item.ErrorMessage));
        }
    }
}
