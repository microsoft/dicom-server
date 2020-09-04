// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.DicomCast.Core.Features.DicomWeb.Service
{
    /// <summary>
    /// Provides functionality to retrieve the change feed from DICOMWeb.
    /// </summary>
    public interface IChangeFeedRetrieveService
    {
        /// <summary>
        /// Asynchronously retrieves the change feed.
        /// </summary>
        /// <param name="offset">Skip events till sequence number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the retrieving operation.</returns>
        Task<IReadOnlyList<ChangeFeedEntry>> RetrieveChangeFeedAsync(long offset, CancellationToken cancellationToken);
    }
}
