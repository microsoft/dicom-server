// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Net;

namespace Microsoft.Health.Fhir.Core.Features.Persistence
{
    public class TransactionFailedException : Exception
    {
        public TransactionFailedException(string message, HttpStatusCode httpStatusCode)
            : base(message)
        {
            Debug.Assert(!string.IsNullOrEmpty(message), "Exception message should not be empty");

            ResponseStatusCode = httpStatusCode;
        }

        public HttpStatusCode ResponseStatusCode { get; }
    }
}
