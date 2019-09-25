// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Health.Dicom.Api.Features.Responses
{
    public class MultipartResult : ActionResult
    {
        private const string MultipartContentSubType = "related";
        private readonly int _statusCode;
        private readonly IEnumerable<MultipartItem> _multipartItems;

        public MultipartResult(int statusCode, IEnumerable<MultipartItem> multipartItems)
        {
            EnsureArg.IsGte(statusCode, 100, nameof(statusCode));
            EnsureArg.IsNotNull(multipartItems, nameof(multipartItems));

            _statusCode = statusCode;
            _multipartItems = multipartItems;
        }

        public static async Task WriteMultipartItemsAsync(HttpResponse httpResponse, IEnumerable<MultipartItem> multipartItems, int statusCode)
        {
            EnsureArg.IsNotNull(httpResponse, nameof(httpResponse));
            EnsureArg.IsNotNull(multipartItems, nameof(multipartItems));
            using (var content = new MultipartContent(MultipartContentSubType))
            {
                foreach (MultipartItem item in multipartItems)
                {
                    content.Add(item.Content);
                    httpResponse.RegisterForDispose(item);
                }

                httpResponse.ContentLength = content.Headers.ContentLength;
                httpResponse.ContentType = content.Headers.ContentType.ToString();
                httpResponse.StatusCode = statusCode;

                await content.CopyToAsync(httpResponse.Body);
            }
        }

        public async override Task ExecuteResultAsync(ActionContext context)
        {
            EnsureArg.IsNotNull(context?.HttpContext?.Response, nameof(context));
            await WriteMultipartItemsAsync(context.HttpContext.Response, _multipartItems, _statusCode);
        }
    }
}
