// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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
                var message = string.Join(Environment.NewLine,
                    context.ModelState.Where(x => x.Value.ValidationState == AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
                     .Select(x => $"\t{x.Key} - {x.Value.Errors.First().ErrorMessage}"));

                throw new InvalidRequestBodyException(Environment.NewLine + message);
            }
        }
    }
}
