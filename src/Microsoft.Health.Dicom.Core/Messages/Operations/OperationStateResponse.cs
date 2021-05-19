// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Messages.Operations
{
    public class OperationStateResponse
    {
        public OperationStateResponse()
        {
        }

        public OperationStateResponse(
            string id,
            OperationType type,
            DateTime createdTime,
            OperationStatus status,
            string errorMessage = null)
        {
            EnsureArg.IsNotNullOrWhiteSpace(id, nameof(id));
            EnsureArg.EnumIsDefined(type, nameof(type));
            EnsureArg.EnumIsDefined(status, nameof(status));

            Id = id;
            Type = type;
            CreatedTime = createdTime;
            Status = status;
            ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage;
        }

        public string Id { get; set; }

        public OperationType Type { get; set; }

        public DateTime CreatedTime { get; set; }

        public OperationStatus Status { get; set; }

        public string ErrorMessage { get; set; }
    }
}
