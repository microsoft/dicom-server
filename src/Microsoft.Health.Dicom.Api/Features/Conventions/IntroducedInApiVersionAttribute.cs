// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Api.Features.Conventions;

/// <summary>
/// Represents the starting API version the controller class was introduced.
/// Don't use any attribute if you want to introduce the controller in all available versions.
/// Did not use ApiVersionAttribute as base because it is a collection of versions and we need just one version here.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class IntroducedInApiVersionAttribute : Attribute
{
    public int? Version { get; init; }
    public IntroducedInApiVersionAttribute(int version)
    {
        Version = version;
    }
}
