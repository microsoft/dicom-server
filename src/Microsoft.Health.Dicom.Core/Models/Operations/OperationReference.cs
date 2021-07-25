// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Models.Operations
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
        /// <para><paramref name="id"/> is <see cref="Guid.Empty"/>.</para>
        /// <para>-or-</para>
        /// <para><paramref name="href"/> is empty or consists of white space characters.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="href"/> is <see langword="null"/>.
        /// </exception>
        public OperationReference(Guid id, Uri href)
        {
            Id = EnsureArg.IsNotEmpty(id, nameof(id));
            Href = EnsureArg.IsNotNull(href, nameof(href));
        }

        /// <summary>
        /// Gets the operation ID.
        /// </summary>
        /// <value>The unique ID that denotes a particular long-running operation.</value>
        [JsonConverter(typeof(JsonGuidConverter), OperationId.FormatSpecifier)]
        public Guid Id { get; }

        /// <summary>
        /// Gets the resource reference for the operation.
        /// </summary>
        /// <value>The unique resource URL for the operation.</value>
        public Uri Href { get; }
    }
}
