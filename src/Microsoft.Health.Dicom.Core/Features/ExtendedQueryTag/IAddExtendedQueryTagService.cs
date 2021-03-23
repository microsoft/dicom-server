// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public interface IAddExtendedQueryTagService
    {
        /// <summary>
        /// Add Extended Query Tags.
        /// </summary>
        /// <param name="extendedQueryTags">The extended query tags.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response.</returns>
        public Task<AddExtendedQueryTagResponse> AddExtendedQueryTagAsync(IEnumerable<ExtendedQueryTagEntry> extendedQueryTags, CancellationToken cancellationToken = default);
    }
}
