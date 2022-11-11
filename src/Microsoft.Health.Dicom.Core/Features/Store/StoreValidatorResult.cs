// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Core.Features.Store;

public sealed class StoreValidatorResult
{
    public StoreValidatorResult(
        IReadOnlyCollection<string> errors,
        IReadOnlyCollection<string> warnings,
        ValidationWarnings validationWarnings,
        Exception firstException)
    {
        Errors = errors;
        Warnings = warnings;
        WarningCodes = validationWarnings;
        FirstException = firstException;
    }

    public IReadOnlyCollection<string> Errors { get; }

    public IReadOnlyCollection<string> Warnings { get; }

    public ValidationWarnings WarningCodes { get; }

    // TODO: Remove this during the cleanup. *** Hack to support the existing validator behavior ***
    public Exception FirstException { get; }
}
