// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Index a dicom instance.
    /// </summary>
    public interface IInstanceIndexer
    {
        /// <summary>
        /// Index instance on custom tags.
        /// </summary>
        /// <param name="tagPathDictionary">The tagPath - CustomTagStoreEntry dictionary.</param>
        /// <param name="instance">The dicom instance.</param>
        /// <param name="cancellationToken">the cancellation token.</param>
        /// <returns>The task.</returns>
        Task IndexInstanceAsync(IDictionary<string, CustomTagStoreEntry> tagPathDictionary, VersionedInstanceIdentifier instance, CancellationToken cancellationToken = default);
    }
}
