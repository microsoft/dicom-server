// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;

namespace Microsoft.Health.Dicom.Core.Features.Validation;
internal static class ValidationUtils
{
    public static bool ContainsControlExceptEsc(string text)
        => text != null && text.Any(c => char.IsControl(c) && (c != '\u001b'));
}
