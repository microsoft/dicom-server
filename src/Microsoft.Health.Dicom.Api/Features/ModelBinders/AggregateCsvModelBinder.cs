// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Health.Dicom.Api.Features.ModelBinders
{
    internal class AggregateCsvModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            EnsureArg.IsNotNull(bindingContext, nameof(bindingContext));
            StringValues values = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).Values;

            IReadOnlyList<string> result = values.Count == 0
                ? Array.Empty<string>()
                : values.SelectMany(x => x.Split(',', StringSplitOptions.TrimEntries)).ToList();

            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }
    }
}
