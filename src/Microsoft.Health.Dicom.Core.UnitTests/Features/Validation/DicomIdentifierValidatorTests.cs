// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using FluentValidation.Validators;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.Store;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class DicomIdentifierValidatorTests
    {
        private static readonly Uri ValidUri = new Uri("http://localhost");

        [Theory]
        [InlineData("1+1")]
        [InlineData("1_1")]
        [InlineData("11|")]
        [InlineData("00000000000000000000000000000000000000000000000000000000000000065")]
        public void GivenAnInvalidDicomId_WhenProcessingARequest_ThenAValidationMessageIsCreated(string id)
        {
            var request = new StoreDicomResourcesRequest(ValidUri, Substitute.For<Stream>(), "test", id);
            IEnumerable<ValidationFailure> result = GetValidationFailures(request);
            Assert.Single(result);
        }

        [Theory]
        [InlineData("1.1")]
        [InlineData("id1")]
        [InlineData("example")]
        [InlineData("a94060e6-038e-411b-a64b-38c2c3ff0fb7")]
        [InlineData("AF30C45C-94AC-4DE3-89D8-9A20BB2A973F")]
        [InlineData("0000000000000000000000000000000000000000000000000000000000000064")]
        public void GivenAValidDicomId_WhenProcessingARequest_ThenAValidationMessageIsNotCreated(string id)
        {
            var request = new StoreDicomResourcesRequest(ValidUri, Substitute.For<Stream>(), "test", id);
            IEnumerable<ValidationFailure> result = GetValidationFailures(request);
            Assert.Empty(result);
        }

        private static IEnumerable<ValidationFailure> GetValidationFailures(StoreDicomResourcesRequest request)
        {
            return new DicomIdentifierValidator().Validate(
                new PropertyValidatorContext(new ValidationContext(request), PropertyRule.Create<StoreDicomResourcesRequest, string>(x => x.StudyInstanceUID), nameof(StoreDicomResourcesRequest.StudyInstanceUID)));
        }
    }
}
