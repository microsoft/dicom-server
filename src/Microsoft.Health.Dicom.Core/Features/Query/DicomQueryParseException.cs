// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class DicomQueryParseException : DicomException
    {
        public DicomQueryParseException(string message)
            : base(message)
        {
        }

        public override HttpStatusCode ResponseStatusCode
        {
            get
            {
                return HttpStatusCode.BadRequest;
            }
        }
    }
}
