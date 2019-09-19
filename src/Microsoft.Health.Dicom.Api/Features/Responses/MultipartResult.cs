// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using EnsureThat;
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

        public async override Task ExecuteResultAsync(ActionContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            var content = new MultipartContent(MultipartContentSubType);

            foreach (MultipartItem item in _multipartItems)
            {
                content.Add(item.Content);
                context.HttpContext.Response.RegisterForDispose(item);
            }

            context.HttpContext.Response.ContentLength = content.Headers.ContentLength;
            context.HttpContext.Response.ContentType = content.Headers.ContentType.ToString();
            context.HttpContext.Response.StatusCode = _statusCode;

            await content.CopyToAsync(context.HttpContext.Response.Body);
        }
    }
}
