// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Jobs
{
    public class ReindexJobEntryInput
    {
        public IEnumerable<string> ExtendedQueryTags { get; set; }

        public long Watermark { get; set; }

        public override string ToString()
        {
            return $"Tags:{string.Join(",", ExtendedQueryTags)}, Watermark: {Watermark}";
        }
    }
}
