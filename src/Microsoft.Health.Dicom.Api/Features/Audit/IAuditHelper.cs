// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace Microsoft.Health.Dicom.Api.Features.Audit;

public interface IAuditHelper
{
    public void LogExecuting(HttpContext httpContext);

    public void LogExecuted(HttpContext httpContext);
}
