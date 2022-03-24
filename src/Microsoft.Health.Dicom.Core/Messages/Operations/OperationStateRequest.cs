// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Operations;

/// <summary>
/// Represents a request for the state of long-running DICOM operations.
/// </summary>
public class OperationStateRequest : IRequest<OperationStateResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationStateRequest"/> class.
    /// </summary>
    /// <param name="operationId">The unique ID for a particular DICOM operation.</param>
    public OperationStateRequest(Guid operationId)
        => OperationId = operationId;

    /// <summary>
    /// Gets the operation ID.
    /// </summary>
    /// <value>The unique ID that denotes a particular operation.</value>
    public Guid OperationId { get; }
}

