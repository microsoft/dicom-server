// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Fhir
{
    /// <summary>
    /// Exception thrown when the transaction fails.
    /// </summary>
    public class TransactionFailedException : FhirNonRetryableException
    {
        public TransactionFailedException(OperationOutcome operationOutcome, Exception innerException)
            : base(DicomCastCoreResource.TransactionFailed, innerException)
        {
            OperationOutcome = operationOutcome;
        }

        public OperationOutcome OperationOutcome { get; }
    }
}
