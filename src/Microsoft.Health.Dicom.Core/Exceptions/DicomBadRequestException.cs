// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using EnsureThat;
using FluentValidation.Results;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class DicomBadRequestException : DicomException
    {
        public DicomBadRequestException(string message)
            : base(message)
        {
        }

        public DicomBadRequestException(IEnumerable<ValidationFailure> validationFailures)
        {
            ValidationFailures = EnsureArg.IsNotNull(validationFailures, nameof(validationFailures));
        }

        public IEnumerable<ValidationFailure> ValidationFailures { get; }

        public override HttpStatusCode ResponseStatusCode => HttpStatusCode.BadRequest;
    }
}
