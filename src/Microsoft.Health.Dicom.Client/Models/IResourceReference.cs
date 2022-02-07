// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Client.Models
{
    /// <summary>
    /// Represents a reference to a RESTful resource.
    /// </summary>
    /// <typeparam name="T">The type of resource.</typeparam>
    public interface IResourceReference<T>
    {
        /// <summary>
        /// Gets the endpoint that returns resources of type <typeparamref name="T"/>.
        /// </summary>
        /// <value>The resource URL.</value>
        Uri Href { get; }
    }
}
