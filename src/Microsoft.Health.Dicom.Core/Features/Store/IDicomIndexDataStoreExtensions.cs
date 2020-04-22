// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Core;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    public static class IDicomIndexDataStoreExtensions
    {
        /// <summary>
        /// Deletes the instance index with no back off on cleaning up the underlying files.
        /// </summary>
        /// <param name="indexDataStore">The IDicomIndexDataStore.</param>
        /// <param name="dicomInstance">The instance to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous delete command.</returns>
        public static async Task DeleteInstanceIndexAsync(this IDicomIndexDataStore indexDataStore, DicomInstanceIdentifier dicomInstance, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
            EnsureArg.IsNotNull(dicomInstance, nameof(dicomInstance));

            await indexDataStore.DeleteInstanceIndexAsync(
                dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid, Clock.UtcNow, Clock.UtcNow, cancellationToken);
        }
    }
}
