// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Extensions
{
    public static class HttpContentExtensions
    {
        public static async Task<Dictionary<MultipartSection, Stream>> ReadAsMultipartDictionaryAsync(this HttpContent content, CancellationToken cancellationToken = default)
        {
            Dictionary<MultipartSection, Stream> result = new Dictionary<MultipartSection, Stream>();
            await using (Stream stream = await content.ReadAsStreamAsync())
            {
                MultipartSection part;
                var media = MediaTypeHeaderValue.Parse(content.Headers.ContentType.ToString());
                var multipartReader = new MultipartReader(HeaderUtilities.RemoveQuotes(media.Boundary).Value, stream, 100);

                while ((part = await multipartReader.ReadNextSectionAsync(cancellationToken)) != null)
                {
                    MemoryStream memStream = new MemoryStream();
                    await part.Body.CopyToAsync(memStream, cancellationToken);
                    memStream.Seek(0, SeekOrigin.Begin);
                    result.Add(part, memStream);
                }
            }

            return result;
        }

        public static async Task<Stream> ReadyAsSeekableStreamAsync(this HttpContent content, CancellationToken cancellationToken = default)
        {
            var memoryStream = new MemoryStream();
            await content.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
    }
}
