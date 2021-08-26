// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Anonymizer.Common.Models
{
    /// <summary>
    /// Indicates types for the input value
    /// Only Date and DateTime can be used for dateshift.
    /// Partial redact is applicable on Age, Date, DateTime and PostalCode.
    /// Any types could be used for cryptoHash, encryption, perturb and redact methods.
    /// </summary>
    public enum AnonymizerValueTypes
    {
        Age,
        Date,
        DateTime,
        PostalCode,
        Others,
    }
}
