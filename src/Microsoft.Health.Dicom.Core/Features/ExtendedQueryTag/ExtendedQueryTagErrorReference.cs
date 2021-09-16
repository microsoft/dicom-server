// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Represents a reference to a one or more extended query tag errors.
    /// </summary>
    public class ExtendedQueryTagErrorReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedQueryTagErrorReference"/> class.
        /// </summary>
        /// <param name="count">The number of errors.</param>
        /// <param name="href">The resource URL for the operation.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="href"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is less than <c>1</c>.
        /// </exception>
        public ExtendedQueryTagErrorReference(int count, Uri href)
        {
            Count = EnsureArg.IsGte(count, 0, nameof(count));
            Href = EnsureArg.IsNotNull(href, nameof(href));
        }

        /// <summary>
        /// Gets the number of errors.
        /// </summary>
        /// <value>The positive number of errors found at the <see cref="Href"/>.</value>
        public int Count { get; }

        /// <summary>
        /// Gets the resource reference for the errors.
        /// </summary>
        /// <value>The unique resource URL for the extended query tag errors.</value>
        public Uri Href { get; }
    }
}
