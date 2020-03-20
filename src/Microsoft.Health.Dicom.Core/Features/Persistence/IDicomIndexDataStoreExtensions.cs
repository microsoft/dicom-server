// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public static class IDicomIndexDataStoreExtensions
    {
        public static async Task DeleteInstanceIndexAsync(this IDicomIndexDataStore indexDataStore, DicomInstance dicomInstance, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
            EnsureArg.IsNotNull(dicomInstance, nameof(dicomInstance));

            await indexDataStore.DeleteInstanceIndexAsync(
                dicomInstance.StudyInstanceUID, dicomInstance.SeriesInstanceUID, dicomInstance.SopInstanceUID, cancellationToken);
        }
    }
}
