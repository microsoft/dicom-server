// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Text.Json;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Health.Dicom.Api.Features.Swagger
{
    /// <summary>
    /// Represents the Swagger/Swashbuckle operation filter used to document the implicit API version parameter.
    /// </summary>
    /// <remarks>This <see cref="IOperationFilter"/> is only required due to bugs in the <see cref="SwaggerGenerator"/>.
    /// Once they are fixed and published, this class can be removed. Fix was found from the samples provided:
    /// https://github.com/microsoft/aspnet-api-versioning/blob/master/samples/aspnetcore/SwaggerSample/SwaggerDefaultValues.cs</remarks>
    public class SwaggerDefaultValues : IOperationFilter
    {
        /// <summary>
        /// Applies the filter to the specified operation using the given context.
        /// </summary>
        /// <param name="operation">The operation to apply the filter to.</param>
        /// <param name="context">The current operation filter context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            EnsureArg.IsNotNull(operation, nameof(operation));
            EnsureArg.IsNotNull(context, nameof(context));

            ApiDescription apiDescription = context.ApiDescription;

            operation.Deprecated |= apiDescription.IsDeprecated();

            // REF: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/1752#issue-663991077
            foreach (ApiResponseType responseType in context.ApiDescription.SupportedResponseTypes)
            {
                // REF: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/b7cf75e7905050305b115dd96640ddd6e74c7ac9/src/Swashbuckle.AspNetCore.SwaggerGen/SwaggerGenerator/SwaggerGenerator.cs#L383-L387
                string responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
                OpenApiResponse response = operation.Responses[responseKey];

                foreach (string contentType in response.Content.Keys)
                {
                    if (!responseType.ApiResponseFormats.Any(x => x.MediaType == contentType))
                    {
                        response.Content.Remove(contentType);
                    }
                }

                operation.Responses[responseKey] = response;
            }

            if (operation.Parameters == null)
            {
                return;
            }

            foreach (OpenApiParameter parameter in operation.Parameters)
            {
                ApiParameterDescription description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

                if (parameter.Description == null)
                {
                    parameter.Description = description.ModelMetadata?.Description;
                }

                if (parameter.Schema.Default == null && description.DefaultValue != null)
                {
                    // REF: https://github.com/Microsoft/aspnet-api-versioning/issues/429#issuecomment-605402330
                    string json = JsonSerializer.Serialize(description.DefaultValue, description.ModelMetadata.ModelType);
                    parameter.Schema.Default = OpenApiAnyFactory.CreateFromJson(json);
                }

                parameter.Required |= description.IsRequired;
            }
        }
    }
}
