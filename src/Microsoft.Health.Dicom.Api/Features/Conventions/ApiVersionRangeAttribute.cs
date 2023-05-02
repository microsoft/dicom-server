// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Health.Dicom.Api.Features.Conventions;

/// <summary>
/// Represents the API version ranges that are applicable to the controller.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class ApiVersionRangeAttribute : Attribute
{
    public ApiVersion Start { get; }

    public ApiVersion End { get; }

    public ApiVersionRangeAttribute(int start = 0, int end = 0)
        : this(start < 1 ? null : new ApiVersion(start, 0), end < 1 ? null : new ApiVersion(end, 0))
    { }

    public ApiVersionRangeAttribute(string start = null, string end = null)
        : this(start != null ? ApiVersion.Parse(start) : null, end != null ? ApiVersion.Parse(end) : null)
    { }

    private ApiVersionRangeAttribute(ApiVersion start, ApiVersion end)
    {
        start ??= DicomApiVersions.Earliest;
        end ??= DicomApiVersions.Latest;

        if (start < DicomApiVersions.Earliest || start >= end)
            throw new ArgumentOutOfRangeException(nameof(start));

        if (end < DicomApiVersions.Earliest || end >= DicomApiVersions.Latest)
            throw new ArgumentOutOfRangeException(nameof(end));

        Start = start;
        End = end;
    }
}
