// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag;

public class DeleteExtendedQueryTagOptions
{
    internal const string SectionName = "DeleteExtendedQueryTag";

    /// <summary>
    /// Gets or sets the <see cref="ActivityRetryOptions"/> for delete extended query tag activities.
    /// </summary>
    public ActivityRetryOptions RetryOptions { get; set; }

    /// <summary>
    /// Gets or sets the number of threads available for each batch.
    /// </summary>
    [Range(-1, int.MaxValue)]
    public int MaxParallelThreads { get; set; } = -1;

    public int BatchSize { get; set; } = 100;
}
