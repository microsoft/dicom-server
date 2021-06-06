// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Operations;

namespace Microsoft.Health.Dicom.Core.Messages.Operations
{
    public class OperationStatusResponse
    {
        public OperationStatusResponse()
        {
        }

        public OperationStatusResponse(
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

        public string Id { get; }

        public OperationType Type { get; }

        public DateTime CreatedTime { get; }

        public OperationStatus Status { get; }

        public string ErrorMessage { get; }
    }
}
