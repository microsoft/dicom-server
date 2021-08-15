// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Configs;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Health.Dicom.Api.Features.Swagger
{
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;
        private readonly SwaggerConfiguration _swaggerConfiguration;

        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, IOptions<SwaggerConfiguration> swaggerConfiguration)
        {
            _provider = EnsureArg.IsNotNull(provider, nameof(provider));
            _swaggerConfiguration = EnsureArg.IsNotNull(swaggerConfiguration?.Value, nameof(swaggerConfiguration));
        }

        public void Configure(SwaggerGenOptions options)
        {
            if (_swaggerConfiguration.ServerUri != null)
            {
                options.AddServer(new OpenApiServer { Url = _swaggerConfiguration.ServerUri.ToString() });
            }

            OpenApiLicense license = null;
            if (!string.IsNullOrWhiteSpace(_swaggerConfiguration.License.Name))
            {
                license = new OpenApiLicense
                {
                    Name = _swaggerConfiguration.License.Name,
                    Url = _swaggerConfiguration.License.Url,
                };
            }
            foreach (ApiVersionDescription description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(
                    description.GroupName,
                    new OpenApiInfo
                    {
                        Title = _swaggerConfiguration.Title,
                        Version = description.ApiVersion.ToString(),
                        License = license,
                    });
            }
        }
    }
}
