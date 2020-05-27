// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Features.Filters
{
    public class AcceptTransferSyntaxFilterAttribute : ActionFilterAttribute
    {
        private const int NotAcceptableResponseCode = (int)HttpStatusCode.NotAcceptable;
        private const string TransferSyntaxHeaderPrefix = "transfer-syntax";

        private readonly HashSet<string> _transferSyntaxes;

        public AcceptTransferSyntaxFilterAttribute(string[] transferSyntaxes)
        {
            Debug.Assert(transferSyntaxes.Length > 0, "The accept transfer syntax filter must have at least one transfer syntax specified.");

            _transferSyntaxes = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (string transferSyntax in transferSyntaxes)
            {
                _transferSyntaxes.Add(transferSyntax.ToUpperInvariant());
            }
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            IList<MediaTypeHeaderValue> acceptHeaders = context.HttpContext.Request.GetTypedHeaders().Accept;
            bool acceptable = false;

            // As model binding happens prior to filteration, use the transfer syntax that was found in TransferSyntaxModelBinder and validate if it is acceptable.
            if (context.ModelState.ContainsKey(TransferSyntaxHeaderPrefix) && _transferSyntaxes.Contains(context.ModelState[TransferSyntaxHeaderPrefix].RawValue))
            {
                acceptable = true;
            }

            if (!acceptable)
            {
                context.Result = new StatusCodeResult(NotAcceptableResponseCode);
            }

            base.OnActionExecuting(context);
        }
    }
}
