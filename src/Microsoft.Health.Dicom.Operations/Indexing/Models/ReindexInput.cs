// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Operations.Durable;

namespace Microsoft.Health.Dicom.Operations.Indexing.Models
{
    internal class ReindexInput
    {
        public IReadOnlyCollection<int> QueryTagKeys { get; set; }

        public WatermarkRange? Completed { get; set; }

        public OperationProgress GetProgress()
        {
            int percentComplete = 0;
            if (Completed.HasValue)
            {
                WatermarkRange range = Completed.GetValueOrDefault();
                percentComplete = range.End == 1 ? 100 : (int)((double)(range.End - range.Start + 1) / range.End * 100);
            }

            return new OperationProgress
            {
                PercentComplete = percentComplete,
                ResourceIds = QueryTagKeys?.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
            };
        }
    }
}
