// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when the service is not available.
    /// </summary>
    /// <remarks>
    /// TODO: This should be moved to common code base.
    /// </remarks>
    public class ServiceUnavailableException : DicomException
    {
        public ServiceUnavailableException()
            : base(DicomCoreResource.ServiceUnavailable)
        {
        }

        public override HttpStatusCode ResponseStatusCode => HttpStatusCode.ServiceUnavailable;
    }
}
