// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Features.Responses
{
    public class MetadataResult : ObjectResult
    {
        private readonly RetrieveMetadataResponse _response;

        public MetadataResult(RetrieveMetadataResponse response)
            : base(response.ResponseMetadata)
        {
            EnsureArg.IsNotNull(response, nameof(response));
            _response = response;
        }

        public async override Task ExecuteResultAsync(ActionContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            ObjectResult result = null;

            // If cache is valid, 304 (Not Modified) status should be returned with no body, else, 200 (OK) status should be returned.
            if (_response.IsCacheValid)
            {
                result = new ObjectResult(null)
                {
                    StatusCode = (int)HttpStatusCode.NotModified,
                };
            }
            else
            {
                result = new ObjectResult(_response.ResponseMetadata)
                {
                    StatusCode = (int)HttpStatusCode.OK,
                };
            }

            // If response contains an ETag, add it to the headers.
            if (!_response.IsCacheValid && !string.IsNullOrEmpty(_response.ETag))
            {
                context.HttpContext.Response.Headers.Add(HeaderNames.ETag, _response.ETag);
            }

            await result.ExecuteResultAsync(context);
        }
    }
}
