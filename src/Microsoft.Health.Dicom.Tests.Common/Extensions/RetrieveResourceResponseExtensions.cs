// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Tests.Common.Extensions
{
    public static class RetrieveResourceResponseExtensions
    {
        public static async Task<IEnumerable<Stream>> GetStreamsAsync(this RetrieveResourceResponse response)
        {
            await using IAsyncEnumerator<RetrieveResourceInstance> enumerator = response.ResponseInstances.GetAsyncEnumerator(CancellationToken.None);
            List<Stream> streams = new List<Stream>();
            while (await enumerator.MoveNextAsync())
            {
                streams.Add(enumerator.Current.Stream);
            }
            return streams;
        }
    }
}
