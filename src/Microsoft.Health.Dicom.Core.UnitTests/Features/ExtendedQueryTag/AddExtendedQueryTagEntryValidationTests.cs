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
            AddExtendedQueryTagEntry addExtendedQueryTagEntry = new AddExtendedQueryTagEntry() { Path = "00101001", QueryTagLevel = "Study" };
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
            AddExtendedQueryTagEntry addExtendedQueryTagEntry = new AddExtendedQueryTagEntry() { Path = pathValue, QueryTagLevel = "Study" };
            var validationContext = new ValidationContext(addExtendedQueryTagEntry);
            IEnumerable<ValidationResult> results = addExtendedQueryTagEntry.Validate(validationContext);
            Assert.Single(results);
            Assert.Equal("The Dicom Tag Property Path must be specified and must not be null, empty or whitespace.", results.First().ErrorMessage);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GivenEmptyNullOrWhitespaceLevel_WhenValidating_ResultShouldHaveExceptions(string levelValue)
        {
            AddExtendedQueryTagEntry addExtendedQueryTagEntry = new AddExtendedQueryTagEntry() { Path = "00101001", QueryTagLevel = levelValue };
            var validationContext = new ValidationContext(addExtendedQueryTagEntry);
            IEnumerable<ValidationResult> results = addExtendedQueryTagEntry.Validate(validationContext);
            Assert.Collection(
                results,
                item => Assert.Equal("The Dicom Tag Property QueryTagLevel must be specified and must not be null, empty or whitespace.", item.ErrorMessage),
                item => Assert.Equal(string.Format("Input Dicom Tag QueryTagLevel '{0}' is invalid. It must have value 'Study', 'Series' or 'Instance'.", levelValue), item.ErrorMessage)
                );
        }

        [Fact]
        public void GivenInvalidLevel_WhenValidating_ResultShouldHaveExceptions()
        {
            AddExtendedQueryTagEntry addExtendedQueryTagEntry = new AddExtendedQueryTagEntry() { Path = "00101001", QueryTagLevel = "Studys" };
            var validationContext = new ValidationContext(addExtendedQueryTagEntry);
            IEnumerable<ValidationResult> results = addExtendedQueryTagEntry.Validate(validationContext);
            Assert.Single(results);
            Assert.Equal("Input Dicom Tag QueryTagLevel 'Studys' is invalid. It must have value 'Study', 'Series' or 'Instance'.", results.First().ErrorMessage);
        }

        [Fact]
        public void GivenMultipleValidationErrors_WhenValidating_ResultShouldHaveExceptions()
        {
            AddExtendedQueryTagEntry addExtendedQueryTagEntry = new AddExtendedQueryTagEntry() { Path = "", QueryTagLevel = " " };
            var validationContext = new ValidationContext(addExtendedQueryTagEntry);
            IEnumerable<ValidationResult> results = addExtendedQueryTagEntry.Validate(validationContext);
            Assert.Collection(
                results,
                item => Assert.Equal("The Dicom Tag Property Path must be specified and must not be null, empty or whitespace.", item.ErrorMessage),
                item => Assert.Equal("The Dicom Tag Property QueryTagLevel must be specified and must not be null, empty or whitespace.", item.ErrorMessage),
                item => Assert.Equal("Input Dicom Tag QueryTagLevel ' ' is invalid. It must have value 'Study', 'Series' or 'Instance'.", item.ErrorMessage)
                );
        }
    }
}
