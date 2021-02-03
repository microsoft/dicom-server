// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public interface ICustomTagService
    {
        /// <summary>
        /// Add Custom Tags.
        /// </summary>
        /// <param name="customTags">The custom tags.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response.</returns>
        public Task<AddCustomTagResponse> AddCustomTagAsync(IEnumerable<CustomTagEntry> customTags, CancellationToken cancellationToken = default);

        public Task DeleteCustomTagAsync(string tagPath, CancellationToken cancellationToken = default);
    }
}
