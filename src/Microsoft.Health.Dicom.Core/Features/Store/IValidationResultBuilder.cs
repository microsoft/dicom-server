// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Store;

public interface IValidationResultBuilder
{
    bool HasWarnings { get; }

    bool HasErrors { get; }

    public IEnumerable<string> Errors { get; }

    public IEnumerable<string> Warnings { get; }

    public ValidationWarnings WarningCodes { get; }

    // TODO: Remove this during the cleanup. *** Hack to support the existing validator behavior ***
    public Exception FirstException { get; }

    void AddError(Exception ex, QueryTag queryTag = null);

    void AddError(string message, QueryTag queryTag = null);

    void AddWarning(ValidationWarnings warningCode, QueryTag queryTag = null);
}
