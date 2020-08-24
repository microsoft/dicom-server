// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Audit
{
    /// <summary>
    /// Value set defined at http://dicom.nema.org/medical/dicom/current/output/html/part15.html#sect_A.5
    /// </summary>
    public static class AuditEventType
    {
        public const string System = "http://dicom.nema.org/medical/dicom/current/output/html/part15.html#sect_A.5";

        public const string RestFulOperationCode = "rest";
    }
}
