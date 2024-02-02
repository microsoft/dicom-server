// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag.Models;

public class DeleteBatchArguments : DeleteExtendedQueryTagArguments
{
    public DeleteBatchArguments(DeleteExtendedQueryTagArguments deleteExtendedQueryTagArguments, WatermarkRange range)
    {
        EnsureArg.IsNotNull(deleteExtendedQueryTagArguments, nameof(deleteExtendedQueryTagArguments));

        Range = range;
        VR = deleteExtendedQueryTagArguments.VR;
        TagKey = deleteExtendedQueryTagArguments.TagKey;
    }

    public WatermarkRange Range { get; set; }
}
