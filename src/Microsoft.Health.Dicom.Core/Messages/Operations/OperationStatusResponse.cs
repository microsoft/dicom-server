// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Core.Messages.Operations
{
    /// <summary>
    /// Represents a response with the status of long-running DICOM operations.
    /// </summary>
    public class OperationStatusResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationStatusResponse"/> class.
        /// </summary>
        /// <param name="operationStatus">The status of the long-running operation.</param>
        public OperationStatusResponse(OperationStatus operationStatus)
            => OperationStatus = EnsureArg.IsNotNull(operationStatus);

        /// <summary>
        /// Gets the status of the long-running operation.
        /// </summary>
        /// <value>The detailed operation status.</value>
        public OperationStatus OperationStatus { get; }
    }
}
