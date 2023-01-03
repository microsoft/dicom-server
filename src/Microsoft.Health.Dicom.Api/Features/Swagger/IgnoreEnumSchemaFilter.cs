// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Health.Dicom.Api.Features.Swagger;

internal class IgnoreEnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(schema, nameof(schema));

        if (context.Type.IsEnum)
        {
            var enumStrings = new List<IOpenApiAny>();
            foreach (var value in Enum.GetValues(context.Type))
            {
                var member = context.Type.GetMember(value.ToString())[0];

                if (!member.GetCustomAttributes<IgnoreEnumAttribute>().Any())
                {
                    enumStrings.Add(new OpenApiString(JsonNamingPolicy.CamelCase.ConvertName(value.ToString())));
                }
            }

            schema.Enum = enumStrings;
        }
    }
}
