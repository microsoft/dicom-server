// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Models.Operations
{
    /// <summary>
    /// A <see langword="static"/> class for utilities related to operation IDs.
    /// </summary>
    public static class OperationId
    {
        /// <summary>
        /// Gets the <see cref="Guid"/> format specifier for normalizing operation ID string values.
        /// </summary>
        public const string FormatSpecifier = "N";

        /// <summary>
        /// Gets the normalized <see cref="string"/> representation for operation IDs.
        /// </summary>
        /// <remarks>
        /// Other <see cref="Guid"/> formats are valid, but the DICOM APIs return a consistent representation.
        /// </remarks>
        /// <param name="operationId">A unique operation ID.</param>
        /// <returns>
        /// The <see cref="string"/> representation for the <paramref name="operationId"/> using
        /// the <c>"N"</c> format specifier.
        /// </returns>
        public static string ToString(Guid operationId)
            => operationId.ToString(FormatSpecifier);
    }
}
