// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Core.Models.Operations
{
    /// <summary>
    /// Represents the internal metadata for a long-running DICOM operation.
    /// </summary>
    internal class InternalOperationStatus : CoreOperationStatus
    {
        /// <summary>
        /// Gets the collection of resources identifiers that the operation is creating or manipulating.
        /// </summary>
        /// <remarks>
        /// The set of resources may change until the <see cref="CoreOperationStatus.Status"/> indicates completion.
        /// </remarks>
        /// <value>One or more resources identifiers.</value>
        public IReadOnlyCollection<string> ResourceIds { get; set; }
    }
}
