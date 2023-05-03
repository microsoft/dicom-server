// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.Health.Dicom.Api.Features.ModelBinders;

internal class MandatoryTimeZoneBinder : IModelBinder
{
    private static readonly Regex TimeZoneSuffix = new Regex(@"Z|(?:[+-]\d{1,2}(?::\d{1,2})?)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

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
            if (TimeZoneSuffix.IsMatch(value) && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset dto))
                bindingContext.Result = ModelBindingResult.Success(dto);
            else
                bindingContext.ModelState.TryAddModelError(modelName, DicomApiResource.TimeZoneRequired);
        }

        return Task.CompletedTask;
    }
}
