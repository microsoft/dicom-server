// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Models.Indexing;

internal class ReindexCheckpoint : ReindexInput, IOperationCheckpoint
{
    public WatermarkRange? Completed { get; set; }

    public DateTime? CreatedTime { get; set; }

    public int? PercentComplete
    {
        get
        {
            if (Completed.HasValue)
            {
                WatermarkRange range = Completed.GetValueOrDefault();
                return range.End == 1 ? 100 : (int)((double)(range.End - range.Start + 1) / range.End * 100);
            }

            return 0;
        }
    }

    public IReadOnlyCollection<string> ResourceIds => QueryTagKeys?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList();

    public IReadOnlyDictionary<string, string> AdditionalProperties => null;
}
