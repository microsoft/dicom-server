// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public interface IDeleteCustomTagService
    {
        /// <summary>
        /// Delete custom tag.
        /// </summary>
        /// <param name="tagPath">The custom tag path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public Task DeleteCustomTagAsync(string tagPath, CancellationToken cancellationToken = default);
    }
}
