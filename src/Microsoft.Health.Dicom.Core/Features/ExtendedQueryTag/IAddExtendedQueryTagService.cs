// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

public interface IAddExtendedQueryTagService
{
    /// <summary>
    /// Add Extended Query Tags.
    /// </summary>
    /// <param name="extendedQueryTags">The extended query tags.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    /// <exception cref="ExistingOperationException">There is already an ongoing re-index operation.</exception>
    /// <exception cref="ExtendedQueryTagsAlreadyExistsException">
    /// One or more values in <paramref name="extendedQueryTags"/> has already been indexed.
    /// </exception>
    public Task<OperationReference> AddExtendedQueryTagsAsync(IEnumerable<AddExtendedQueryTagEntry> extendedQueryTags, CancellationToken cancellationToken = default);
}
