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

        private readonly HashSet<string> _transferSyntaxes;
        private readonly bool _allowMissing;

        public AcceptTransferSyntaxFilterAttribute(string[] transferSyntaxes, bool allowMissing = false)
        {
            Debug.Assert(transferSyntaxes.Length > 0, "The accept transfer syntax filter must have at least one transfer syntax specified.");
            _allowMissing = allowMissing;
            _transferSyntaxes = new HashSet<string>(transferSyntaxes, StringComparer.InvariantCultureIgnoreCase);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            bool acceptable;
            if (context.ModelState.TryGetValue(TransferSyntaxHeaderPrefix, out ModelStateEntry transferSyntaxValue))
            {
                acceptable = _transferSyntaxes.Contains(transferSyntaxValue.RawValue);
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
