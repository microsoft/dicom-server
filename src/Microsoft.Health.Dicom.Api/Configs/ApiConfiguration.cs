// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Api.Configuration;
using Microsoft.Health.Api.Features.Cors;

namespace Microsoft.Health.Dicom.Api.Configs;

public class ApiConfiguration : IApiConfiguration
{
    public CorsConfiguration Cors { get; } = new CorsConfiguration();

    public SwaggerConfiguration Swagger { get; } = new SwaggerConfiguration();
}
