// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag.Models;

public class DeleteBatchArguments : DeleteExtendedQueryTagArguments
{
    public DeleteBatchArguments(int tagKey, string vr, WatermarkRange range)
    {
        Range = range;
        VR = vr;
        TagKey = tagKey;
    }

    public WatermarkRange Range { get; set; }
}
