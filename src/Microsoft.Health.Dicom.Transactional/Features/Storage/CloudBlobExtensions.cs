// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Health.Dicom.Transactional.Features.Storage.Models;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Transactional.Features.Storage
{
    internal static class CloudBlobExtensions
    {
        private static JsonSerializer _jsonSerializer = new JsonSerializer();

        public static async Task<TransactionMessage> ReadTransactionMessageAsync(this ICloudBlob cloudBlob, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(cloudBlob, nameof(cloudBlob));

            using (Stream stream = await cloudBlob.OpenReadAsync(cancellationToken))
            using (var streamReader = new StreamReader(stream, TransactionMessage.MessageEncoding))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                return _jsonSerializer.Deserialize<TransactionMessage>(jsonTextReader);
            }
        }
    }
}
