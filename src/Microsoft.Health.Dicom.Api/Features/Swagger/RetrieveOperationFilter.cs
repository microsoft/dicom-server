// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Health.Dicom.Api.Features.Swagger
{
    public class RetrieveOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            EnsureArg.IsNotNull(operation, nameof(operation));
            EnsureArg.IsNotNull(context, nameof(context));

            if (operation.OperationId != null && operation.OperationId.Contains("retrieve", StringComparison.OrdinalIgnoreCase))
            {
                foreach (ApiResponseType responseType in context.ApiDescription.SupportedResponseTypes)
                {
                    if (responseType.StatusCode == 200)
                    {
                        string responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();

                        OpenApiResponse response = operation.Responses[responseKey];

                        response.Content.Clear();

                        if (operation.OperationId.EndsWith("Instance", StringComparison.OrdinalIgnoreCase))
                        {
                            response.Content.Add("application/dicom", new OpenApiMediaType());
                        }

                        response.Content.Add("multipart/related", new OpenApiMediaType());
                    }
                }
            }
        }
    }
}
