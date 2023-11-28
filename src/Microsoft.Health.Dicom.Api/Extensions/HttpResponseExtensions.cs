// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Extensions;

internal static class HttpResponseExtensions
{
    public const string ErroneousAttributesHeader = "erroneous-dicom-attributes";
    private const string WarningHeaderPattern = "{0} {1} \"{2}\"";
    private const string UnknownAgentHost = "-";

    // ExampleRoot is necessary as GetComponents, like many of the URI members, throws an exception
    // when used on relative URI instances. As a workaround, we use it to help perform operations on relative URIs.
    private static readonly Uri ExampleRoot = new Uri("https://example.com/", UriKind.Absolute);

    public static void AddLocationHeader(this HttpResponse response, Uri locationUrl)
    {
        EnsureArg.IsNotNull(response, nameof(response));
        EnsureArg.IsNotNull(locationUrl, nameof(locationUrl));

        response.Headers.Append(HeaderNames.Location, locationUrl.IsAbsoluteUri ? locationUrl.AbsoluteUri : GetRelativeUri(locationUrl));
    }

    /// <summary>
    /// Set Response Warning header.
    /// </summary>
    /// <param name="response">The httpResponse.</param>
    /// <param name="code">Warning code.</param>
    /// <param name="host">Host name.</param>
    /// <param name="message">The warning message.</param>
    public static void SetWarning(this HttpResponse response, HttpWarningCode code, string host, string message)
    {
        EnsureArg.IsNotNull(response, nameof(response));
        EnsureArg.IsNotEmptyOrWhiteSpace(message, nameof(message));

        if (string.IsNullOrWhiteSpace(host))
        {
            host = UnknownAgentHost;
        }

        response.Headers.Warning = string.Format(CultureInfo.InvariantCulture, WarningHeaderPattern, (int)code, host, message);
    }

    public static bool TryAddErroneousAttributesHeader(this HttpResponse response, IReadOnlyCollection<string> erroneousAttributes)
    {
        EnsureArg.IsNotNull(response, nameof(response));
        EnsureArg.IsNotNull(erroneousAttributes, nameof(erroneousAttributes));
        if (erroneousAttributes.Count == 0)
        {
            return false;
        }

        response.Headers.Append(ErroneousAttributesHeader, string.Join(",", erroneousAttributes));
        return true;
    }

    private static string GetRelativeUri(Uri uri)
        => new Uri(ExampleRoot, uri).GetComponents(UriComponents.AbsoluteUri & ~UriComponents.SchemeAndServer & ~UriComponents.UserInfo, UriFormat.UriEscaped);
}
