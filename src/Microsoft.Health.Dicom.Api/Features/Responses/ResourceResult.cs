// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Dicom.Api.Web;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Features.Responses;

internal class ResourceResult : IActionResult
{
    private readonly RetrieveResourceResponse _response;
    private readonly RetrieveConfiguration _retrieveConfiguration;

    internal ResourceResult(RetrieveResourceResponse response, RetrieveConfiguration retrieveConfiguration)
    {
        _response = EnsureArg.IsNotNull(response);
        _retrieveConfiguration = EnsureArg.IsNotNull(retrieveConfiguration);
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        ObjectResult objectResult;
        if (_response.IsSinglePart)
        {
            objectResult = await GetSinglePartResult(context.HttpContext, context.HttpContext.RequestAborted);
        }
        else
        {
            objectResult = GetMultiPartResult(context.HttpContext, context.HttpContext.RequestAborted);
        }
        await objectResult.ExecuteResultAsync(context);
    }

    private async Task<ObjectResult> GetSinglePartResult(HttpContext context, CancellationToken cancellationToken)
    {
        var enumerator = _response.GetResponseInstancesEnumerator(cancellationToken);
        var enumResult = await enumerator.MoveNextAsync();

        Debug.Assert(enumResult, "Failed to get the item in Enumerator.");

        Stream stream = enumerator.Current.Stream;
        string transferSyntax = enumerator.Current.TransferSyntaxUid;
        context.Response.RegisterForDispose(stream);
        var objectResult = new ObjectResult(stream)
        {
            StatusCode = (int)HttpStatusCode.OK,
        };

        var singlePartMediaType = new MediaTypeHeaderValue(_response.ContentType);
        singlePartMediaType.Parameters.Add(new NameValueHeaderValue(KnownContentTypes.TransferSyntax, transferSyntax));

        objectResult.ContentTypes.Add(singlePartMediaType);
        return objectResult;
    }

    private ObjectResult GetMultiPartResult(HttpContext context, CancellationToken cancellationToken)
    {
        string boundary = Guid.NewGuid().ToString();
        var mediaType = new MediaTypeHeaderValue(KnownContentTypes.MultipartRelated);
        mediaType.Parameters.Add(new NameValueHeaderValue(KnownContentTypes.Boundary, boundary));
#pragma warning disable CA2000 // Dispose objects before losing scope, registered for dispose in response
        LazyMultipartReadOnlyStream lazyStream = new LazyMultipartReadOnlyStream(
            GetAsyncEnumerableStreamContent(context, _response.GetResponseInstancesEnumerator(cancellationToken), _response.ContentType),
            boundary,
            _retrieveConfiguration.LazyResponseStreamBufferSize,
            cancellationToken);
#pragma warning restore CA2000 // Dispose objects before losing scope
        context.Response.RegisterForDispose(lazyStream);
        var result = new ObjectResult(lazyStream)
        {
            StatusCode = (int)HttpStatusCode.OK,
        };
        result.ContentTypes.Add(mediaType);
        return result;
    }

    private static async IAsyncEnumerable<DicomStreamContent> GetAsyncEnumerableStreamContent(
        HttpContext context,
        IAsyncEnumerator<RetrieveResourceInstance> lazyInstanceEnumerator,
        string contentType)
    {
        while (await lazyInstanceEnumerator.MoveNextAsync())
        {
            context.Response.RegisterForDispose(lazyInstanceEnumerator.Current.Stream);
            yield return
                new DicomStreamContent()
                {
                    Stream = lazyInstanceEnumerator.Current.Stream,
                    StreamLength = lazyInstanceEnumerator.Current.StreamLength,
                    Headers = new List<KeyValuePair<string, IEnumerable<string>>>()
                    {
                        new KeyValuePair<string, IEnumerable<string>>(KnownContentTypes.ContentType, new []{ $"{contentType}; {KnownContentTypes.TransferSyntax}={lazyInstanceEnumerator.Current.TransferSyntaxUid}"})
                    }
                };
        }
    }
}
