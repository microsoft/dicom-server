// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag.Models;

public class BatchCreationArguments : DeleteExtendedQueryTagArguments
{
    public BatchCreationArguments(DeleteExtendedQueryTagArguments deleteExtendedQueryTagArguments, int batchSize, int batchCount)
    {
        EnsureArg.IsNotNull(deleteExtendedQueryTagArguments, nameof(deleteExtendedQueryTagArguments));

        BatchSize = batchSize;
        BatchCount = batchCount;
        TagKey = deleteExtendedQueryTagArguments.TagKey;
        VR = deleteExtendedQueryTagArguments.VR;
    }

    public int BatchSize { get; set; }

    public int BatchCount { get; set; }
}
