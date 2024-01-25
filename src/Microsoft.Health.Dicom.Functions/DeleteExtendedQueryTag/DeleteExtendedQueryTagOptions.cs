// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag;

public class DeleteExtendedQueryTagOptions
{
    /// <summary>
    /// Gets or sets the <see cref="ActivityRetryOptions"/> for re-indexing activities.
    /// </summary>
    public ActivityRetryOptions RetryOptions { get; set; }

    public int BatchSize { get; set; }
}
