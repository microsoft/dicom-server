// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Client.Models
{
    /// <summary>
    /// Represents a reference to a one or more extended query tag errors.
    /// </summary>
    public class ExtendedQueryTagErrorReference
    {
        /// <summary>
        /// Gets or sets the number of errors.
        /// </summary>
        /// <value>The positive number of errors found at the <see cref="Href"/>.</value>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the resource reference for the errors.
        /// </summary>
        /// <value>The unique resource URL for the extended query tag errors.</value>
        public Uri Href { get; set; }
    }
}
