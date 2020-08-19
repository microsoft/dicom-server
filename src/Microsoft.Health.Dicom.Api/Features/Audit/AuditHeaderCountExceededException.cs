// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Features.Audit
{
    public class AuditHeaderCountExceededException : AuditHeaderException
    {
        public AuditHeaderCountExceededException(int size)
            : base(string.Format(DicomApiResource.TooManyCustomAuditHeaders, AuditConstants.MaximumNumberOfCustomHeaders, size))
        {
        }
    }
}
