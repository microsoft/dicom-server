// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Api.Features.Filters
{
    public class ModelStateValidatorAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                throw new InvalidQueryStringValuesException(context.ModelState.FirstOrDefault(x => x.Value.Errors.Any()).Key);
            }
        }
    }
}
