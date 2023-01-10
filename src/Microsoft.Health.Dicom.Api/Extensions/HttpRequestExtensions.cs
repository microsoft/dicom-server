// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Extensions;

public static class HttpRequestExtensions
{
    public static IReadOnlyCollection<AcceptHeader> GetAcceptHeaders(this HttpRequest httpRequest)
    {
        EnsureArg.IsNotNull(httpRequest, nameof(httpRequest));
        IList<MediaTypeHeaderValue> acceptHeaders = httpRequest.GetTypedHeaders().Accept;

        return acceptHeaders?.Count > 0
            ? acceptHeaders.Select(item => item.ToAcceptHeader()).ToList()
            : Array.Empty<AcceptHeader>();
    }

    /// <summary>
    /// Get host name from httpRequest
    /// </summary>
    /// <param name="httpRequest">The httpRequest.</param>
    /// <param name="dicomStandards">True if follow dicom standards, false otherwise.</param>
    /// <returns>The host.</returns>
    public static string GetHost(this HttpRequest httpRequest, bool dicomStandards = false)
    {
        EnsureArg.IsNotNull(httpRequest, nameof(httpRequest));
        string host = httpRequest.Host.Host;
        if (dicomStandards && !string.IsNullOrWhiteSpace(host))
        {
            // As Dicom standard, should append colon after service. https://dicom.nema.org/medical/dicom/current/output/chtml/part18/sect_11.7.3.2.html
            host = host + ":";
        }
        return host;
    }
}
