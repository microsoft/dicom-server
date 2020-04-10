// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class DicomInstanceMetadataNotFoundException : DicomException
    {
        public DicomInstanceMetadataNotFoundException()
            : base()
        {
        }

        public override HttpStatusCode ResponseStatusCode => HttpStatusCode.NotFound;
    }
}
