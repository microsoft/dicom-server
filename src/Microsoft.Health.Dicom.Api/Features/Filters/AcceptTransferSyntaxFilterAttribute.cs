// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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

            _transferSyntaxes = new HashSet<string>();

            foreach (var transferSyntax in transferSyntaxes)
            {
                _transferSyntaxes.Add(transferSyntax.ToUpperInvariant());
            }
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var acceptHeader = context.HttpContext.Request.Headers["Accept"];

            bool acceptable = false;
            bool hasTransferSyntaxHeader = false;
            List<string> acceptHeaders = new List<string>();

            // Validate the accept headers has one of the specified accepted transfer syntaxes.
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
                    hasTransferSyntaxHeader = true;

                    if (_transferSyntaxes.Any(x => splitAcceptHeaderValue[1].Trim().Equals(x, System.StringComparison.InvariantCultureIgnoreCase)))
                    {
                        acceptable = true;
                    }
                }
            }

            if (hasTransferSyntaxHeader && !acceptable)
            {
                context.Result = new StatusCodeResult(NotAcceptableResponseCode);
            }

            base.OnActionExecuting(context);
        }
    }
}
