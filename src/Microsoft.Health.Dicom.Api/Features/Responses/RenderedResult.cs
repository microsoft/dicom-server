// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Features.Responses;
internal class RenderedResult : IActionResult
{
    private readonly RetrieveRenderedResponse _response;

    internal RenderedResult(RetrieveRenderedResponse response)
    {
        _response = EnsureArg.IsNotNull(response);
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        Stream stream = _response.ResponseStream;
        context.HttpContext.Response.RegisterForDispose(stream);
        var objectResult = new ObjectResult(stream)
        {
            StatusCode = (int)HttpStatusCode.OK,
        };
        var mediaType = new MediaTypeHeaderValue(_response.ContentType);
        objectResult.ContentTypes.Add(mediaType);

        await objectResult.ExecuteResultAsync(context);
    }
}
