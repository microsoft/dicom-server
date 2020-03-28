// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions
{
    public class DicomInvalidMediaTypeException : DicomException
    {
        public DicomInvalidMediaTypeException()
            : base()
        {
        }

        public override HttpStatusCode ResponseStatusCode
        {
            get
            {
                return HttpStatusCode.UnsupportedMediaType;
            }
        }
    }
}
