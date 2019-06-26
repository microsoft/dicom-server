// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Polly;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage
{
    public static class CloudBlockBlobExtensions
    {
        public static async Task<T> ThrowDataStoreException<T>(this CloudBlockBlob cloudBlockBlob, Func<CloudBlockBlob, Task<T>> action, IAsyncPolicy retryPolicy = null)
        {
            EnsureArg.IsNotNull(cloudBlockBlob, nameof(cloudBlockBlob));
            EnsureArg.IsNotNull(action, nameof(action));

            try
            {
                if (retryPolicy != null)
                {
                    return await retryPolicy.ExecuteAsync(() => action(cloudBlockBlob));
                }

                return await action(cloudBlockBlob);
            }
            catch (StorageException e)
            {
                throw new DataStoreException(e.RequestInformation.HttpStatusCode, e);
            }
        }

        public static async Task ThrowDataStoreException(this CloudBlockBlob documentClient, Func<CloudBlockBlob, Task> action, IAsyncPolicy retryPolicy = null)
        {
            EnsureArg.IsNotNull(documentClient, nameof(documentClient));
            EnsureArg.IsNotNull(action, nameof(action));

            try
            {
                if (retryPolicy != null)
                {
                    await retryPolicy.ExecuteAsync(() => action(documentClient));
                }
                else
                {
                    await action(documentClient);
                }
            }
            catch (StorageException e)
            {
                throw new DataStoreException(e.RequestInformation.HttpStatusCode, e);
            }
        }
    }
}
