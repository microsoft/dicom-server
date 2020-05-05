// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Core;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    public static class IIndexDataStoreExtensions
    {
        /// <summary>
        /// Deletes the instance index with no back off on cleaning up the underlying files.
        /// </summary>
        /// <param name="indexDataStore">The IDicomIndexDataStore.</param>
        /// <param name="instance">The instance to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous delete command.</returns>
        public static async Task DeleteInstanceIndexAsync(this IIndexDataStore indexDataStore, InstanceIdentifier instance, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
            EnsureArg.IsNotNull(instance, nameof(instance));

            await indexDataStore.DeleteInstanceIndexAsync(
                instance.StudyInstanceUid, instance.SeriesInstanceUid, instance.SopInstanceUid, Clock.UtcNow, cancellationToken);
        }
    }
}
