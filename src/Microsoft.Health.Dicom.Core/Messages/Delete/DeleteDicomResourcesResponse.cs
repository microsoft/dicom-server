// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;

namespace Microsoft.Health.Dicom.Core.Messages.Delete
{
    public sealed class DeleteDicomResourcesResponse : BaseStatusCodeResponse
    {
        public DeleteDicomResourcesResponse(int statusCode)
            : base(statusCode)
        {
        }

        public DeleteDicomResourcesResponse(HttpStatusCode statusCode)
            : base((int)statusCode)
        {
        }
    }
}
