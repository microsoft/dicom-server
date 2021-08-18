// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Client.Models
{
    /// <summary>
    /// Represents a reference to an existing long-running oepration.
    /// </summary>
    public class OperationReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationReference"/> class.
        /// </summary>
        /// <param name="id">The unique operation ID.</param>
        /// <param name="href">The resource URL for the operation.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="href"/> is empty or consists of white space characters.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="href"/> is <see langword="null"/>.
        /// </exception>
        public OperationReference(string id, Uri href)
        {
            Id = id;
            Href = EnsureArg.IsNotNull(href, nameof(href));
        }

        /// <summary>
        /// Gets the operation ID.
        /// </summary>
        /// <value>The unique ID that denotes a particular long-running operation.</value>
        public string Id { get; }

        /// <summary>
        /// Gets the resource reference for the operation.
        /// </summary>
        /// <value>The unique resource URL for the operation.</value>
        public Uri Href { get; }
    }
}
