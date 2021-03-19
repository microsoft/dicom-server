// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    /// <summary>
    /// Different type of  Validation Errors.
    /// </summary>
    internal enum ValidationError
    {
        /// <summary>
        /// No error.
        /// </summary>
        NoError,

        /// <summary>
        /// The value exceed max allowed length.
        /// </summary>
        ExceedMaxLength,

        /// <summary>
        /// The value contains invalid character(s).
        /// </summary>
        ContainsInvalidChar,
    }
}
