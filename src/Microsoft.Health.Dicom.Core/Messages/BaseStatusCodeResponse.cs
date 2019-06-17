// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;

namespace Microsoft.Health.Dicom.Core.Messages
{
    public abstract class BaseStatusCodeResponse
    {
        public BaseStatusCodeResponse(HttpStatusCode statusCode)
        {
            StatusCode = (int)statusCode;
        }

        public int StatusCode { get; }
    }
}
