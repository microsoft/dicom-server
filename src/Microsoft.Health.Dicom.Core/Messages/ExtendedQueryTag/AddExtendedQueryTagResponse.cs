// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class AddExtendedQueryTagResponse
    {
        public AddExtendedQueryTagResponse(string operationId)
        {
            EnsureArg.IsNotNullOrWhiteSpace(operationId);
            OperationId = operationId;
        }

        public string OperationId { get; }
    }
}
