// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Client.Models
{
    /// <summary>
    /// Represents a reference to an existing long-running oepration.
    /// </summary>
    public class OperationReference
    {
        /// <summary>
        /// Gets or sets the operation ID.
        /// </summary>
        /// <value>The unique ID that denotes a particular long-running operation.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the resource reference for the operation.
        /// </summary>
        /// <value>The unique resource URL for the operation.</value>
        public Uri Href { get; set; }
    }
}
