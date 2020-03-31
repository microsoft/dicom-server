// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    [Serializable]
    internal class DicomInstanceNotFoundException : DicomException
    {
        public DicomInstanceNotFoundException()
           : base()
        {
        }

        public override HttpStatusCode ResponseStatusCode
        {
            get
            {
                return HttpStatusCode.NotFound;
            }
        }
    }
}
