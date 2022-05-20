// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Models.Operations;

/// <summary>
/// Represents the state of a long-running operation with checkpoint information.
/// </summary>
/// <typeparam name="T">The type used to denote the category of operation.</typeparam>
public class OperationCheckpointState<T>
{
    /// <summary>
    /// Gets or sets the operation ID.
    /// </summary>
    /// <value>The unique ID that denotes a particular operation.</value>
    public Guid OperationId { get; init; }

    /// <summary>
    /// Gets or sets the category of the operation.
    /// </summary>
    public T Type { get; init; }

    /// <summary>
    /// Gets or sets the execution status of the operation.
    /// </summary>
    public OperationStatus Status { get; init; }

    /// <summary>
    /// Gets or sets the operation's checkpoint.
    /// </summary>
    public IOperationCheckpoint Checkpoint { get; init; }
}
