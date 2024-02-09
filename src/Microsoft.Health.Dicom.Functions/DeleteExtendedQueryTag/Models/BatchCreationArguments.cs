// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag.Models;

public class BatchCreationArguments : DeleteExtendedQueryTagArguments
{
    public BatchCreationArguments(int tagKey, string vr, int batchSize, int batchCount)
    {
        EnsureArg.IsGte(batchSize, 1, nameof(batchSize));
        EnsureArg.IsGte(batchCount, 1, nameof(batchCount));

        BatchSize = batchSize;
        BatchCount = batchCount;
        TagKey = tagKey;
        VR = vr;
    }

    public int BatchSize { get; set; }

    public int BatchCount { get; set; }
}
