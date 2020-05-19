// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.Health.Dicom.Api.Features.ModelBinders
{
    public class TransferSyntaxModelBinder : IModelBinder
    {
        private const string TransferSyntaxHeaderPrefix = "transfer-syntax";

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var acceptHeader = bindingContext.HttpContext.Request.Headers["Accept"];

            List<string> acceptHeaders = new List<string>();

            if (acceptHeader.Count > 0)
            {
                char[] headerValueDelimiters = new char[] { ';', ',' };
                foreach (var value in acceptHeader)
                {
                    acceptHeaders.AddRange(value.Split(headerValueDelimiters));
                }
            }

            foreach (string acceptHeaderValue in acceptHeaders)
            {
                var splitAcceptHeaderValue = acceptHeaderValue.Split("=");
                if (splitAcceptHeaderValue.Length == 2 && splitAcceptHeaderValue[0].Trim().Equals(TransferSyntaxHeaderPrefix, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    bindingContext.Result = ModelBindingResult.Success(splitAcceptHeaderValue[1].Trim());
                    return Task.CompletedTask;
                }
            }

            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }
    }
}
