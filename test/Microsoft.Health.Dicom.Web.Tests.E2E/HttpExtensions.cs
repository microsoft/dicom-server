// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Web.Tests.E2E
{
    public static class HttpExtensions
    {
        public static async Task<IEnumerable<Stream>> ReadMultipartResponseAsStreamsAsync(this HttpContent httpContent)
        {
            EnsureArg.IsNotNull(httpContent, nameof(httpContent));

            var result = new List<Stream>();
            using (Stream stream = await httpContent.ReadAsStreamAsync())
            {
                MultipartSection part;
                var media = MediaTypeHeaderValue.Parse(httpContent.Headers.ContentType.ToString());
                var multipartReader = new MultipartReader(HeaderUtilities.RemoveQuotes(media.Boundary).Value, stream, 100);

                while ((part = await multipartReader.ReadNextSectionAsync()) != null)
                {
                    var memoryStream = new MemoryStream();
                    await part.Body.CopyToAsync(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    result.Add(memoryStream);
                }
            }

            return result;
        }
    }
}
