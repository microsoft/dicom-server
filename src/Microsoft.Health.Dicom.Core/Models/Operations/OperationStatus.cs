// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Core.Models.Operations
{
    /// <summary>
    /// Represents the metadata for a long-running DICOM operation.
    /// </summary>
    public class OperationStatus : CoreOperationStatus
    {
        /// <summary>
        /// Gets the collection of resources locations that the operation is creating or manipulating.
        /// </summary>
        /// <remarks>
        /// The set of resources may change until the <see cref="CoreOperationStatus.Status"/> indicates completion.
        /// </remarks>
        /// <value>One or more resources URIs.</value>
        public IReadOnlyCollection<Uri> Resources { get; set; }
    }
}
