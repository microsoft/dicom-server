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
    internal abstract class CsvModelBinder<T> : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            EnsureArg.IsNotNull(bindingContext, nameof(bindingContext));
            StringValues values = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).Values;

            if (values.Count == 0)
            {
                bindingContext.Result = ModelBindingResult.Success(Array.Empty<T>());
            }
            else if (values.Count > 1)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, DicomApiResource.DuplicateParameter);
            }
            else if (string.IsNullOrEmpty(values[0]))
            {
                bindingContext.Result = ModelBindingResult.Success(Array.Empty<T>());
            }
            else
            {
                string[] split = values[0].Split(',', StringSplitOptions.TrimEntries);
                T[] parsed = new T[split.Length];
                for (int i = 0; i < split.Length; i++)
                {
                    if (!TryParse(split[i], out T parsedValue))
                    {
                        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, string.Format(DicomApiResource.InvalidParse, split[i], typeof(T).Name));
                        return Task.CompletedTask;
                    }

                    parsed[i] = parsedValue;
                }

                bindingContext.Result = ModelBindingResult.Success(parsed);
            }

            return Task.CompletedTask;
        }

        protected abstract bool TryParse(string value, out T result);
    }
}
