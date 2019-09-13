// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Core.Features.Transaction
{
    public static class TransactionExtensions
    {
        public static async Task DeleteInstancesAsync(
            this ITransactionMessage transactionMessage,
            IDicomBlobDataStore dicomBlobDataStore,
            IDicomMetadataStore dicomMetadataStore,
            IDicomInstanceMetadataStore dicomInstanceMetadataStore,
            IDicomIndexDataStore dicomIndexDataStore,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(transactionMessage, nameof(transactionMessage));
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));
            EnsureArg.IsNotNull(dicomInstanceMetadataStore, nameof(dicomInstanceMetadataStore));
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));

            DicomInstance[] instances = transactionMessage.Instances.ToArray();

            if (instances.Length > 0)
            {
                // Attempt to delete the instance indexes and instance metadata.
                await Task.WhenAll(
                    dicomIndexDataStore.DeleteInstancesIndexAsync(throwOnNotFound: false, cancellationToken, instances),
                    dicomMetadataStore.DeleteInstanceAsync(throwOnNotFound: false, cancellationToken, instances));

                await Task.WhenAll(instances.Select(async x =>
                {
                    await dicomInstanceMetadataStore.DeleteInstanceMetadataIfExistsAsync(x, cancellationToken);
                    await dicomBlobDataStore.DeleteInstanceIfExistsAsync(x, cancellationToken);
                }));
            }
        }
    }
}
