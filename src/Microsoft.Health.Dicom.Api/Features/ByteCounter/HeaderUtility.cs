// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Health.Dicom.Api.Features.ByteCounter
{
    public static class HeaderUtility
    {
        private static readonly Encoding HeaderEncoding = Encoding.UTF8;
        private static readonly int HeaderDelimiterByteCount = HeaderEncoding.GetByteCount(": ");
        private static readonly int HeaderEndOfLineCharactersByteCount = HeaderEncoding.GetByteCount("\r\n");

        public static int GetTotalHeaderLength(IHeaderDictionary headers)
        {
            // Per https://en.wikipedia.org/wiki/List_of_HTTP_header_fields, each header will be of the form
            // headerKey: headerValues, and terminated by an end-of-line character sequence. The list of headers
            // will be terminated by another end-of-line character sequence.
            EnsureArg.IsNotNull(headers, nameof(headers));

            int headerLength = 0;
            foreach (KeyValuePair<string, StringValues> header in headers)
            {
                headerLength += HeaderEncoding.GetByteCount(header.Key)
                    + HeaderDelimiterByteCount
                    + HeaderEncoding.GetByteCount(header.Value.ToString())
                    + HeaderEndOfLineCharactersByteCount;
            }

            headerLength += HeaderEndOfLineCharactersByteCount;

            return headerLength;
        }
    }
}
