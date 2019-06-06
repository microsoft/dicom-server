// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FluentValidation;
using FluentValidation.Results;
using MediatR.Pipeline;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public class ValidateRequestPreProcessor<TRequest> : IRequestPreProcessor<TRequest>
        where TRequest : class
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidateRequestPreProcessor(IEnumerable<IValidator<TRequest>> validators)
        {
            EnsureArg.IsNotNull(validators, nameof(validators));

            _validators = validators;
        }

        public async Task Process(TRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            ValidationResult[] allResults = await Task.WhenAll(_validators.Select(x => x.ValidateAsync(request)));
            ValidationResult[] validationFailures = allResults.Where(x => x != null && !x.IsValid).ToArray();

            if (validationFailures.Length > 0)
            {
                throw new DicomBadRequestException(validationFailures.SelectMany(x => x.Errors));
            }
        }
    }
}
