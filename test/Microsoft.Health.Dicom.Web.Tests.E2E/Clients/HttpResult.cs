// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Clients
{
    public class HttpResult<T>
    {
        public HttpResult(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        public HttpResult(HttpStatusCode statusCode, T value)
            : this(statusCode)
        {
            Value = value;
        }

        public HttpStatusCode StatusCode { get; }

        public T Value { get; }
    }
}
