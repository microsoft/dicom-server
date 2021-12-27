// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.WorkitemMessages
{
    public class WorkitemStoreRequest : IRequest<WorkitemStoreResponse>
    {
        public WorkitemStoreRequest(
            Stream requestBody,
            string requestContentType,
            string workItemInstanceUid = null,
            string transactionUid = null)
        {
            WorkitemInstanceUid = workItemInstanceUid;
            WorkitemTransactionUid = transactionUid;
            RequestBody = requestBody;
            RequestContentType = requestContentType;
        }

        public string WorkitemInstanceUid { get; }

        public string WorkitemTransactionUid { get; }

        public Stream RequestBody { get; }

        public string RequestContentType { get; }
    }
}
