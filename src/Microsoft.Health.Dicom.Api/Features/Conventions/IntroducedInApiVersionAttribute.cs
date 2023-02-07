// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Api.Features.Conventions;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class IntroducedInApiVersionAttribute : Attribute
{
    public int? Version { get; init; }
    public IntroducedInApiVersionAttribute(int verion)
    {
        Version = verion;
    }
}
