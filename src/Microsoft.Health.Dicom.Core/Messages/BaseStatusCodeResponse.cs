// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Messages
{
    public abstract class BaseStatusCodeResponse
    {
        private const int MinimumStatusCode = 100;

        public BaseStatusCodeResponse(int statusCode)
        {
            EnsureArg.IsGte(statusCode, MinimumStatusCode, nameof(statusCode));

            StatusCode = statusCode;
        }

        public int StatusCode { get; }
    }
}
