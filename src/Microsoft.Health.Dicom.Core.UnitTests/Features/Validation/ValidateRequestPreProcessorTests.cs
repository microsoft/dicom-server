// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.Store;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class ValidateRequestPreProcessorTests
    {
        [Fact]
        public async Task GivenARequest_WhenValidatingThatType_ThenAllValidationRulesShouldRun()
        {
            AbstractValidator<StoreDicomResourcesRequest> mockValidator1 = Substitute.For<AbstractValidator<StoreDicomResourcesRequest>>();
            AbstractValidator<StoreDicomResourcesRequest> mockValidator2 = Substitute.For<AbstractValidator<StoreDicomResourcesRequest>>();

            AbstractValidator<StoreDicomResourcesRequest>[] validators = new[] { mockValidator1, mockValidator2 };
            var validationHandler = new ValidateRequestPreProcessor<StoreDicomResourcesRequest>(validators);
            StoreDicomResourcesRequest request = CreateTestStoreDicomResourcesRequest();

            await validationHandler.Process(request, CancellationToken.None);

            await mockValidator1.Received().ValidateAsync(Arg.Any<ValidationContext<StoreDicomResourcesRequest>>(), Arg.Any<CancellationToken>());
            await mockValidator2.Received().ValidateAsync(Arg.Any<ValidationContext<StoreDicomResourcesRequest>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenARequest_WhenValidatingThatTypeWithFailingRule_ThenAValidationExceptionShouldBeThrown()
        {
            AbstractValidator<StoreDicomResourcesRequest> mockValidator1 = Substitute.For<AbstractValidator<StoreDicomResourcesRequest>>();
            AbstractValidator<StoreDicomResourcesRequest> mockValidator2 = Substitute.For<AbstractValidator<StoreDicomResourcesRequest>>();

            AbstractValidator<StoreDicomResourcesRequest>[] validators = new[] { mockValidator1, mockValidator2 };
            var validationHandler = new ValidateRequestPreProcessor<StoreDicomResourcesRequest>(validators);
            StoreDicomResourcesRequest request = CreateTestStoreDicomResourcesRequest();

            var validationError = Task.FromResult(new ValidationResult(new[] { new ValidationFailure("Id", "Id should not be null") }));

            mockValidator2
                .ValidateAsync(Arg.Any<ValidationContext<StoreDicomResourcesRequest>>(), Arg.Any<CancellationToken>())
                .Returns(validationError);

            await Assert.ThrowsAsync<DicomBadRequestException>(async () => await validationHandler.Process(request, CancellationToken.None));
        }

        private static StoreDicomResourcesRequest CreateTestStoreDicomResourcesRequest()
        {
            return new StoreDicomResourcesRequest(new Uri("http://localhost"), Substitute.For<Stream>(), string.Empty);
        }
    }
}
