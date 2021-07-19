// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Functions.Indexing.Models
{
    internal class ReindexInput
    {
        public IReadOnlyCollection<int> QueryTagKeys { get; set; }

        public ReindexProgress Progress { get; set; }
    }
}
