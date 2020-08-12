// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Api.Extensions
{
    public static class HttpContextExtensions
    {
        public static IEnumerable<AcceptHeader> GetAcceptHeaders(this HttpContext httpContext)
        {
            IList<AcceptHeader> result = new List<AcceptHeader>();
            var acceptHeaders = httpContext.Request.GetTypedHeaders().Accept;

            if (acceptHeaders != null && acceptHeaders.Count > 0)
            {
                foreach (var acceptHeader in acceptHeaders)
                {
                    AcceptHeader accept = new AcceptHeader(acceptHeader.MediaType.Value);
                    foreach (var parameter in acceptHeader.Parameters)
                    {
                        string name = parameter.Name.Value;
                        string value = parameter.Value.Value;
                        if (!accept.Parameters.ContainsKey(name))
                        {
                            accept.Parameters.Add(name, value);
                        }
                    }

                    result.Add(accept);
                }
            }

            return result;
        }
    }
}
