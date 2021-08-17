// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.OpenApi.Models;

namespace Microsoft.Health.Dicom.Api.Configs
{
    public class SwaggerConfiguration
    {
        public Uri ServerUri { get; set; }

        public string Title { get; set; } = "Medical Imaging Server for DICOM";

        public OpenApiLicense License { get; } = new OpenApiLicense();
    }
}
