// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Models.Operations;

/// <summary>
/// Represents a set of search criteria when querying for potentially multiple operations.
/// </summary>
public sealed class OperationQueryCondition<T>
{
    /// <summary>
    /// Gets the optional collection of operation types to include in the search result.s
    /// </summary>
    /// <value>Zero or more operation types.</value>
    public IEnumerable<T> Operations { get; init; }

    /// <summary>
    /// Gets the optional collection of operation statues to include in the search results.
    /// </summary>
    /// <value>Zero or more status.</value>
    public IEnumerable<OperationStatus> Statuses { get; init; }

    /// <summary>
    /// Gets the optional minimum value for the <see cref="OperationState{T}.CreatedTime"/> property
    /// to include in the search results.
    /// </summary>
    /// <value>The minimum operation created time if specified; otherwise <see cref="DateTime.MinValue"/>.</value>
    public DateTime CreatedTimeFrom { get; init; } = DateTime.MinValue;

    /// <summary>
    /// Gets the optional maximum value for the <see cref="OperationState{T}.CreatedTime"/> property
    /// to include in the search results.
    /// </summary>
    /// <value>The maximum operation created time if specified; otherwise <see cref="DateTime.MaxValue"/>.</value>
    public DateTime CreatedTimeTo { get; init; } = DateTime.MaxValue;

}
