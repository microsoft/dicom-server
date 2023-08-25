// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Health.Dicom.Api.Features.Filters;

public sealed class QueryModelStateValidatorAttribute : ActionFilterAttribute
{ 
    private static readonly Regex HtmlCharacters = new Regex("<[^>]*>", RegexOptions.Compiled);

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        if (!context.ModelState.IsValid)
        {
            (string key, ModelStateEntry value) = context.ModelState.Where(x => x.Value.Errors.Count > 0).First();

            string errorMessage = value.Errors[0].ErrorMessage;
            if (!errorMessage.IsNullOrEmpty() && HtmlCharacters.IsMatch(errorMessage))
            {
                errorMessage = HttpUtility.HtmlEncode(errorMessage);
            }

            throw new InvalidQueryStringValuesException(key, errorMessage);
        }
    }
}
