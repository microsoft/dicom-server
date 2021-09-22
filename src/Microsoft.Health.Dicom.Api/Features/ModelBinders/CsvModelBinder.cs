// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Health.Dicom.Api.Features.ModelBinders
{
    internal class CsvModelBinder : IModelBinder
    {
        protected virtual object DefaultValue { get; } = Array.Empty<string>();

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            EnsureArg.IsNotNull(bindingContext, nameof(bindingContext));
            StringValues values = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).Values;

            if (values.Count == 0)
            {
                bindingContext.Result = ModelBindingResult.Success(DefaultValue);
            }
            else if (values.Count > 1)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, DicomApiResource.DuplicateValues);
            }
            else if (string.IsNullOrEmpty(values[0]))
            {
                bindingContext.Result = ModelBindingResult.Success(DefaultValue);
            }
            else if (TryParse(values[0].Split(',', StringSplitOptions.TrimEntries), out object result))
            {
                bindingContext.Result = ModelBindingResult.Success(result);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, string.Format(DicomApiResource.InvalidParse, values[0]));
            }

            return Task.CompletedTask;
        }

        protected virtual bool TryParse(string[] values, out object result)
        {
            result = values;
            return true;
        }
    }
}
