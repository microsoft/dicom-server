// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;

namespace Microsoft.Health.Dicom.Core.Messages.Store
{
    public sealed class StoreDicomResourcesResponse : BaseStatusCodeResponse
    {
        public StoreDicomResourcesResponse(HttpStatusCode statusCode)
            : base(statusCode)
        {
        }
    }
}
