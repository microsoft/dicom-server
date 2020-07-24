// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Features.ModelBinders
{
    public class IfNoneMatchModelBinder : IModelBinder
    {
        private readonly string ifNoneMatchHeaderPrefix;

        public IfNoneMatchModelBinder()
        {
            ifNoneMatchHeaderPrefix = HeaderNames.IfNoneMatch;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            bindingContext.HttpContext.Request.Headers.TryGetValue(ifNoneMatchHeaderPrefix, out StringValues ifNoneMatchValues);

            string ifNoneMatch = ifNoneMatchValues.FirstOrDefault();

            if (!string.IsNullOrEmpty(ifNoneMatch))
            {
                ValueProviderResult valueProviderResult = new ValueProviderResult(ifNoneMatch);
                bindingContext.ModelState.SetModelValue(ifNoneMatchHeaderPrefix, valueProviderResult);
                bindingContext.Result = ModelBindingResult.Success(ifNoneMatch);
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }
    }
}
