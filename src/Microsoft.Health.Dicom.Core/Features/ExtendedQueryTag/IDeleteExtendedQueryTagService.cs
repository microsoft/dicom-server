// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public interface IDeleteExtendedQueryTagService
    {
        /// <summary>
        /// Delete extended query tag.
        /// </summary>
        /// <param name="tagPath">The extended query tag path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public Task DeleteExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken = default);
    }
}
