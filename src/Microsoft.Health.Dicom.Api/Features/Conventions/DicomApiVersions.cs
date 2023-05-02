// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Health.Dicom.Api.Features.Conventions;

internal static class DicomApiVersions
{
    public static readonly ApiVersion V1Prelease = ApiVersion.Parse("1.0-prerelease");
    public static readonly ApiVersion V1 = new ApiVersion(2, 0);
    public static readonly ApiVersion V2 = new ApiVersion(2, 0);

    public static readonly ApiVersion Earliest = V1Prelease;
    public static readonly ApiVersion Latest = V2;

    public static IEnumerable<ApiVersion> GetApiVersions(bool includeUnstable)
    {
        // this will result in null minor instead of 0 minor. There is no constructor on ApiVersion that allows this directly
        yield return V1Prelease;
        yield return V1;

        if (includeUnstable)
            yield return V2;
    }
}
