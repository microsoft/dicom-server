// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;

namespace Microsoft.Health.Dicom.Core.Web
{
    /// <summary>
    /// Provides functionality to create a new instance of <see cref="IMultipartReader"/>.
    /// </summary>
    public interface IMultipartReaderFactory
    {
        /// <summary>
        /// Creates a new instance of <see cref="IMultipartReader"/>.
        /// </summary>
        /// <param name="contentType">The request content type.</param>
        /// <param name="body">The request body.</param>
        /// <returns>An instance of <see cref="IMultipartReader"/>.</returns>
        IMultipartReader Create(string contentType, Stream body);
    }
}
