// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public abstract class DicomException : Exception
    {
        public DicomException()
        {
        }

        public DicomException(string message)
            : base(message)
        {
        }

        public DicomException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public abstract HttpStatusCode ResponseStatusCode { get; }
    }
}
