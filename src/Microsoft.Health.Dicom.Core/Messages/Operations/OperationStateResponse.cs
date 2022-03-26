// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Messages.Operations;

/// <summary>
/// Represents a response with the state of long-running DICOM operations.
/// </summary>
public class OperationStateResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationStateResponse"/> class.
    /// </summary>
    /// <param name="operationState">The state of the long-running operation.</param>
    public OperationStateResponse(OperationState<DicomOperation> operationState)
        => OperationState = EnsureArg.IsNotNull(operationState);

    /// <summary>
    /// Gets the state of the long-running operation.
    /// </summary>
    /// <value>The detailed operation state.</value>
    public OperationState<DicomOperation> OperationState { get; }
}
