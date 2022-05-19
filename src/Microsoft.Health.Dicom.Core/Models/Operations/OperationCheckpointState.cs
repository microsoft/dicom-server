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
    public Guid OperationId { get; init; }

    public T Type { get; init; }

    public OperationStatus Status { get; init; }

    public IOperationCheckpoint Checkpoint { get; init; }
}
