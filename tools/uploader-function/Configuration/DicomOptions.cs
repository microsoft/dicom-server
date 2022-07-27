// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Client.Authentication;

namespace DicomUploaderFunction.Configuration;

public class DicomOptions
{
    public const string SectionName = "DicomWeb";

    public Uri Endpoint { get; set; }

    public AuthenticationOptions Authentication { get; set; }
}
