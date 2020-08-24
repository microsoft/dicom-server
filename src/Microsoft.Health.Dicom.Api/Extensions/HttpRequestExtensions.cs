// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Extensions
{
    public static class HttpRequestExtensions
    {
        public static IEnumerable<AcceptHeader> GetAcceptHeaders(this HttpRequest httpRequest)
        {
            EnsureArg.IsNotNull(httpRequest, nameof(httpRequest));
            IList<MediaTypeHeaderValue> acceptHeaders = httpRequest.GetTypedHeaders().Accept;

            if (acceptHeaders != null && acceptHeaders.Count != 0)
            {
                return acceptHeaders.Select((item) => item.ToAcceptHeader())
                    .ToList();
            }

            return Enumerable.Empty<AcceptHeader>();
        }
    }
}
