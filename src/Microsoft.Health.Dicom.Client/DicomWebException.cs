// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using EnsureThat;

namespace Microsoft.Health.Dicom.Client
{
    public class DicomWebException : Exception
    {
        public DicomWebException(DicomWebResponse response)
        {
            EnsureArg.IsNotNull(response, nameof(response));

            Response = response;
        }

        public HttpStatusCode StatusCode => Response.StatusCode;

        public HttpResponseHeaders Headers => Response.Headers;

        public HttpContent Content => Response.Content;

        protected DicomWebResponse Response { get; }

        public override string Message
            => $"{StatusCode}";
    }
}
