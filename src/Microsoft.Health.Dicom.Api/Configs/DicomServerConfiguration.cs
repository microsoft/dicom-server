// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Api.Configuration;
using Microsoft.Health.Api.Features.Cors;
using Microsoft.Health.Core.Configs;
using Microsoft.Health.Dicom.Core.Configs;

namespace Microsoft.Health.Dicom.Api.Configs
{
    public class DicomServerConfiguration : IApiConfiguration
    {
        public FeatureConfiguration Features { get; } = new FeatureConfiguration();

        public SecurityConfiguration Security { get; } = new SecurityConfiguration();

        public CorsConfiguration Cors { get; } = new CorsConfiguration();

        public ServicesConfiguration Services { get; } = new ServicesConfiguration();

        public AuditConfiguration Audit { get; } = new AuditConfiguration("X-MS-AZUREDICOM-AUDIT-");

        public SwaggerConfiguration Swagger { get; } = new SwaggerConfiguration();
    }
}
