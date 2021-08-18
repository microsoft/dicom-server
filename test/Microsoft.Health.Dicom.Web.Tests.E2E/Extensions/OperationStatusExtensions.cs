// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Extensions
{
    internal static class OperationStatusExtensions
    {
        private static readonly HashSet<OperationRuntimeStatus> InProgressStatuses = new HashSet<OperationRuntimeStatus>() {
            OperationRuntimeStatus.Pending,
            OperationRuntimeStatus.Running
        };

        public static bool IsInProgress(this OperationStatus operationStatus)
        {
            EnsureArg.IsNotNull(operationStatus, nameof(operationStatus));
            return InProgressStatuses.Contains(operationStatus.Status);
        }
    }
}
