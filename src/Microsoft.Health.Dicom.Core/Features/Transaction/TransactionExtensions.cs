// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Core.Features.Transaction
{
    public static class TransactionExtensions
    {
        public static async Task AbortStoreInstancesAsync(
            this ITransaction transaction,
            IDicomBlobDataStore dicomBlobDataStore,
            IDicomMetadataStore dicomMetadataStore,
            IDicomInstanceMetadataStore dicomInstanceMetadataStore,
            IDicomIndexDataStore dicomIndexDataStore,
            CancellationToken cancellationToken = default)
        {
            DicomInstance[] instances = transaction.Instances.ToArray();

            // Attempt to delete the instance indexes and instance metadata.
            await Task.WhenAll(
                dicomIndexDataStore.DeleteInstancesIndexAsync(throwOnNotFound: false, cancellationToken, instances),
                dicomMetadataStore.DeleteInstanceAsync(throwOnNotFound: false, cancellationToken, instances));

            await Task.WhenAll(instances.Select(async x =>
            {
                await dicomInstanceMetadataStore.DeleteInstanceMetadataAsync(x, cancellationToken);
                await dicomBlobDataStore.DeleteInstanceIfExistsAsync(x, cancellationToken);
            }));

            // If the store is rolled back, we commit the transaction.
            await transaction.CommitAsync(cancellationToken);
        }
    }
}
