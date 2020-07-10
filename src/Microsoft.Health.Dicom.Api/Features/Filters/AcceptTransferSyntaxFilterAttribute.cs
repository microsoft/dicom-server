// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.Health.Dicom.Api.Features.Filters
{
    public class AcceptTransferSyntaxFilterAttribute : ActionFilterAttribute
    {
        private const int NotAcceptableResponseCode = (int)HttpStatusCode.NotAcceptable;
        private const string TransferSyntaxHeaderPrefix = "transfer-syntax";
        private readonly bool _allowMissing;
        private readonly HashSet<string> _transferSyntaxes;

        public AcceptTransferSyntaxFilterAttribute(string[] transferSyntaxes, bool allowMissing = false)
        {
            Debug.Assert(transferSyntaxes.Length > 0, "The accept transfer syntax filter must have at least one transfer syntax specified.");
            _transferSyntaxes = new HashSet<string>(transferSyntaxes, StringComparer.InvariantCultureIgnoreCase);
            _allowMissing = allowMissing;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            bool acceptable;

            // As model binding happens prior to filteration, use the transfer syntax that was found in TransferSyntaxModelBinder and validate if it is acceptable.
            if (context.ModelState.TryGetValue(TransferSyntaxHeaderPrefix, out ModelStateEntry transferSyntaxValue))
            {
                if (_transferSyntaxes.Contains(transferSyntaxValue.RawValue))
                {
                    acceptable = true;
                }
                else
                {
                    acceptable = _allowMissing && string.IsNullOrWhiteSpace($"{transferSyntaxValue.RawValue}");
                }
            }
            else
            {
                acceptable = _allowMissing;
            }

            if (!acceptable)
            {
                context.Result = new StatusCodeResult(NotAcceptableResponseCode);
            }

            base.OnActionExecuting(context);
        }
    }
}
