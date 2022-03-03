// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Workitem
{
    public sealed class RetrieveWorkitemRequest : IRequest<RetrieveWorkitemResponse>
    {
        public RetrieveWorkitemRequest(string workitemInstanceUid)
        {
            WorkitemInstanceUid = workitemInstanceUid;
        }

        public string WorkitemInstanceUid { get; }
    }
}
