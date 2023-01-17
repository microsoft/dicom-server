// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Store;

public sealed class StoreValidationResult
{
    public StoreValidationResult(
        IReadOnlyCollection<string> warnings,
        ValidationWarnings validationWarnings,
        IReadOnlyDictionary<DicomTag, StoreErrorResult> invalidTagErrors)
    {
        Warnings = warnings;
        WarningCodes = validationWarnings;
        InvalidTagErrors = invalidTagErrors;
    }

    public IReadOnlyCollection<string> Warnings { get; }

    public ValidationWarnings WarningCodes { get; }

    public IReadOnlyDictionary<DicomTag, StoreErrorResult> InvalidTagErrors { get; }
}
