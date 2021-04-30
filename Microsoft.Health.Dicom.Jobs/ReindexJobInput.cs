// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Jobs
{
    public class ReindexJobInput
    {
        public IEnumerable<string> ExtendedQueryTags { get; set; }
        public long MaxWatermark { get; set; }
    }
}
