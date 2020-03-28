// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class DicomIdentifierInvalidException : DicomException
    {
        public DicomIdentifierInvalidException(string value, string name)
            : base(string.Format(DicomCoreResource.DicomIdentifierInvalid, name, value))
        {
        }

        public override HttpStatusCode ResponseStatusCode => HttpStatusCode.BadRequest;
    }
}
