// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Api.Features.Audit
{
    public class AuditException : DicomServerException
    {
        public AuditException(string controllerName, string actionName)
            : base(string.Format(DicomApiResource.MissingAuditInformation, controllerName, actionName))
        {
        }
    }
}
