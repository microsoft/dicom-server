// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public interface ICustomTagIndexService
    {
        /// <summary>
        /// Add custom tag indexes into store.
        /// </summary>
        /// <param name="customTagIndexes">The index dictionary. Key is custom tag key, value is DicomItem.</param>
        /// <param name="instanceIdentifier">The instance identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>the task.</returns>
        Task AddCustomTagIndexes(Dictionary<long, DicomItem> customTagIndexes, VersionedInstanceIdentifier instanceIdentifier, CancellationToken cancellationToken = default);
    }
}
