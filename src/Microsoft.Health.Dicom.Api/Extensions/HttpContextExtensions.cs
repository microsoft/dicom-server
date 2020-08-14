// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Extensions
{
    public static class HttpContextExtensions
    {
        public static IEnumerable<AcceptHeader> GetAcceptHeaders(this HttpContext httpContext)
        {
            IList<MediaTypeHeaderValue> acceptHeaders = httpContext.Request.GetTypedHeaders().Accept;

            if (acceptHeaders != null)
            {
                return acceptHeaders.Select((item) => item.ToAcceptHeader())
                    .ToList();
            }

            return new List<AcceptHeader>();
        }
    }
}
