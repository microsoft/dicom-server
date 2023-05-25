// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.Health.Dicom.Api.Features.ModelBinders;

internal class MandatoryTimeZoneBinder : IModelBinder
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
            if (!string.IsNullOrWhiteSpace(value))
            {
                try
                {
                    // Attempt to parse the string as a DateTime and convert it to UTC.
                    // The only reason DateTime is used is because the Kind property allows us to
                    // correctly determine whether time zone was specified. Otherwise, we'd have to
                    // construct a series of complicated regular expressions.
                    var dt = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.None);
                    if (dt.Kind == DateTimeKind.Unspecified)
                        bindingContext.ModelState.TryAddModelError(modelName, DicomApiResource.TimeZoneRequired);
                    else
                        bindingContext.Result = ModelBindingResult.Success(new DateTimeOffset(dt.ToUniversalTime()));
                }
                catch (FormatException e)
                {
                    bindingContext.ModelState.TryAddModelException(modelName, e);
                }
            }
        }

        return Task.CompletedTask;
    }
}
