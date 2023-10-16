// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

/// <summary>
/// Used to pass args to cleanup tasks
/// </summary>
public sealed class CleanupBlobArgumentsV2
{
    /// <summary>
    /// Instances that need to be cleaned up
    /// </summary>
    public IReadOnlyList<InstanceMetadata> Instances { get; }

    /// <summary>
    /// Partition within which the instances that need to be cleaned up reside
    /// </summary>
    public Partition Partition { get; }

    /// <summary>
    /// Create cleanup args
    /// </summary>
    /// <param name="instances">Instances that need to be cleaned up</param>
    /// <param name="partition">Partition within which the instances that need to be cleaned up reside</param>
    public CleanupBlobArgumentsV2(IReadOnlyList<InstanceMetadata> instances, Partition partition)
    {
        Instances = EnsureArg.IsNotNull(instances, nameof(instances));
        Partition = EnsureArg.IsNotNull(partition, nameof(partition));
    }
}