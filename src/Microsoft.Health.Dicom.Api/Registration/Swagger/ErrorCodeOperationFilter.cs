// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Health.Dicom.Api.Registration.Swagger
{
    public class ErrorCodeOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            EnsureArg.IsNotNull(operation, nameof(operation));
            EnsureArg.IsNotNull(context, nameof(context));

            foreach (ApiResponseType responseType in context.ApiDescription.SupportedResponseTypes)
            {
                if (responseType.StatusCode == 400 || responseType.StatusCode == 404 || responseType.StatusCode == 406 || responseType.StatusCode == 415)
                {
                    string responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();

                    OpenApiResponse response = operation.Responses[responseKey];

                    foreach (string contentType in response.Content.Keys)
                    {
                        if (response.Content.Count == 1)
                        {
                            OpenApiMediaType value = response.Content[contentType];
                            response.Content.Remove(contentType);
                            response.Content.Add("text/json", value);
                            break;
                        }

                        response.Content.Remove(contentType);
                    }

                    operation.Responses[responseKey] = response;
                }
            }
        }
    }
}
