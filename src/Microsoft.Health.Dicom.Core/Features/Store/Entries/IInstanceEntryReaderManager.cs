// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Store.Entries
{
    /// <summary>
    /// Provides functionality to find the appropriate <see cref="IInstanceEntryReader"/>.
    /// </summary>
    public interface IInstanceEntryReaderManager
    {
        /// <summary>
        /// Finds the appropriate <see cref="IInstanceEntryReader"/> that can read <paramref name="contentType"/>.
        /// </summary>
        /// <param name="contentType">The content type.</param>
        /// <returns>An instance of <see cref="IInstanceEntryReader"/> if found; otherwise, <c>null</c>.</returns>
        IInstanceEntryReader FindReader(string contentType);
    }
}
