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
    private static readonly string[] Formats = new string[]
    {
        // Time Zone (eg. -07:00)
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffzzz",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffffzzz",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffzzz",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffzzz",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffzzz",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffzzz",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fzzz",
        "yyyy'-'MM'-'dd'T'HH':'mm':'sszzz",

        // Time Zone (eg. -07)
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffzz",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffffzz",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffzz",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffzz",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffzz",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffzz",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fzz",
        "yyyy'-'MM'-'dd'T'HH':'mm':'sszz",

        // Explicit UTC
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff'Z'",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffff'Z'",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffff'Z'",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffff'Z'",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ff'Z'",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'f'Z'",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'",
    };

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
            if (DateTimeOffset.TryParseExact(value, Formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dto))
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
