// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Features.ModelBinders
{
    public class TransferSyntaxModelBinder : IModelBinder
    {
        private const string TransferSyntaxHeaderPrefix = "transfer-syntax";

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            IList<MediaTypeHeaderValue> acceptHeaders = bindingContext.HttpContext.Request.GetTypedHeaders().Accept;

            // Validate the accept headers has one of the specified accepted media types.
            if (acceptHeaders != null && acceptHeaders.Count > 0)
            {
                foreach (MediaTypeHeaderValue acceptHeader in acceptHeaders)
                {
                    List<NameValueHeaderValue> typeParameterValue = acceptHeader.Parameters.Where(
                        parameter => StringSegment.Equals(parameter.Name, TransferSyntaxHeaderPrefix, StringComparison.InvariantCultureIgnoreCase)).ToList();

                    if (typeParameterValue.Count > 1)
                    {
                        throw new BadRequestException("Transfer Syntax parameter is specified more than once");
                    }

                    if (typeParameterValue != null && typeParameterValue.Count == 1)
                    {
                        StringSegment parsedValue = HeaderUtilities.RemoveQuotes(typeParameterValue.First().Value);

                        ValueProviderResult valueProviderResult = new ValueProviderResult(parsedValue.ToString());
                        bindingContext.ModelState.SetModelValue(TransferSyntaxHeaderPrefix, valueProviderResult);
                        bindingContext.Result = ModelBindingResult.Success(parsedValue.ToString());
                        return Task.CompletedTask;
                    }
                }
            }

            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }
    }
}
