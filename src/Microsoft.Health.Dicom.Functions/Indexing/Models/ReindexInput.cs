// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.Indexing.Models
{
    internal class ReindexInput
    {
        public IReadOnlyCollection<int> QueryTagKeys { get; set; }

        public WatermarkRange Completed { get; set; }
    }
}
