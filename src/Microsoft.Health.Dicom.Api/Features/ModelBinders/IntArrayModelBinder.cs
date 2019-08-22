// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.Health.Dicom.Api.Features.ModelBinders
{
    public class IntArrayModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ValueProviderResult value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                bindingContext.Result = ModelBindingResult.Success(Array.Empty<int>());
                return Task.CompletedTask;
            }

            var split = value.ToString().Split(',');
            var resultArray = new int[split.Length];

            for (var i = 0; i < split.Length; i++)
            {
                if (!int.TryParse(split[i], out resultArray[i]))
                {
                    bindingContext.Result = ModelBindingResult.Failed();
                    return Task.CompletedTask;
                }
            }

            bindingContext.Result = ModelBindingResult.Success(resultArray);
            return Task.CompletedTask;
        }
    }
}
