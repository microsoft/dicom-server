// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides property accessors for <see cref="FhirTransactionRequestEntry"/> and <see cref="FhirTransactionResponseEntry"/>.
    /// </summary>
    public interface IFhirTransactionRequestResponsePropertyAccessors
    {
        /// <summary>
        /// Gets list of property accessors for <see cref="FhirTransactionRequestEntry"/> and <see cref="FhirTransactionResponseEntry"/>.
        /// </summary>
        IReadOnlyList<FhirTransactionRequestResponsePropertyAccessor> PropertyAccessors { get; }
    }
}
