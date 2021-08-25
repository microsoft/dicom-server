// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Anonymizer.Core.Models
{
    public enum AnonymizerMethod
    {
        Redact,
        DateShift,
        CryptoHash,
        Keep,
        Perturb,
        Encrypt,
        Remove,
        RefreshUID,
        Substitute,
    }
}
