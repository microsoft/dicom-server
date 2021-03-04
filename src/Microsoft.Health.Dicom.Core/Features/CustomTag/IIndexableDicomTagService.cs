// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Cache current custom tag entries.
    /// </summary>
    public interface IIndexableDicomTagService
    {
        Task<IReadOnlyCollection<IndexableDicomTag>> GetIndexableDicomTagsAsync(CancellationToken cancellationToken = default);
    }
}
