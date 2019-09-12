// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Core.Features.Transaction
{
    public static class DeleteTransactionHelper
    {
        public static async Task DeleteInstancesAsync(
            IDicomBlobDataStore dicomBlobDataStore,
            IDicomMetadataStore dicomMetadataStore,
            IDicomInstanceMetadataStore dicomInstanceMetadataStore,
            IDicomIndexDataStore dicomIndexDataStore,
            IEnumerable<DicomInstance> dicomInstances,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));
            EnsureArg.IsNotNull(dicomInstanceMetadataStore, nameof(dicomInstanceMetadataStore));
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));

            // Delete per series grouping
            foreach (IGrouping<string, DicomInstance> grouping in dicomInstances.GroupBy(x => x.StudyInstanceUID + x.SeriesInstanceUID))
            {
                DicomInstance[] instances = grouping.ToArray();

                // Attempt to delete the instance indexes and instance metadata.
                await Task.WhenAll(
                    dicomIndexDataStore.DeleteInstancesIndexAsync(throwOnNotFound: false, cancellationToken, instances),
                    dicomMetadataStore.DeleteInstanceAsync(throwOnNotFound: false, cancellationToken, instances));

                await Task.WhenAll(instances.Select(async x =>
                {
                    await dicomInstanceMetadataStore.DeleteInstanceMetadataAsync(x, cancellationToken);
                    await dicomBlobDataStore.DeleteInstanceIfExistsAsync(x, cancellationToken);
                }));
            }
        }
    }
}
