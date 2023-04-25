// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.Health.Dicom.Api.Models.Binding;

internal class Iso8601Binder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
            throw new ArgumentNullException(nameof(bindingContext));

        string modelName = bindingContext.ModelName;
        ValueProviderResult result = bindingContext.ValueProvider.GetValue(modelName);
        if (result != ValueProviderResult.None)
        {
            bindingContext.ModelState.SetModelValue(modelName, result);

            string value = result.FirstValue;
            if (DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dto))
            {
                bindingContext.Result = ModelBindingResult.Success(dto);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    modelName,
                    string.Format(CultureInfo.CurrentCulture, DicomApiResource.InvalidIso8601DateTime, value));
            }
        }

        return Task.CompletedTask;
    }
}
