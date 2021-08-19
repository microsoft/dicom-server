// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    /// <summary>
    /// Validator that validates DicomDataset for reindexing.
    /// </summary>
    public interface IReindexDatasetValidator
    {
        /// <summary>
        /// Validate <paramref name="dataset"/>.
        /// </summary>
        /// <param name="dataset">The dicom Dataset.</param>
        /// <param name="watermark">The Dicom instance watermark.</param>
        /// <param name="queryTags">The query tags.</param>
        /// <returns>Valid query tags.</returns>
        IReadOnlyCollection<QueryTag> Validate(DicomDataset dataset, long watermark, IReadOnlyCollection<QueryTag> queryTags);
    }
}
