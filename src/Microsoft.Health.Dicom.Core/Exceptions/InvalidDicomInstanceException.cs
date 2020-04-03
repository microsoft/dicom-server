// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when the DICOM instance is invalid.
    /// </summary>
    public class InvalidDicomInstanceException : DicomException
    {
        public InvalidDicomInstanceException()
        {
        }

        public override HttpStatusCode ResponseStatusCode => HttpStatusCode.BadRequest;
    }
}
