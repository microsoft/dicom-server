// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Health.Dicom.Api.Web;

namespace Microsoft.Health.Dicom.Api.Features.Filters
{
    public sealed class BodyModelStateValidatorAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            if (!context.ModelState.IsValid)
            {
                var error = context.ModelState.First(x => x.Value.ValidationState == AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid);
                throw new InvalidRequestBodyException(error.Key, error.Value.Errors.First().ErrorMessage);
            }
        }
    }
}
