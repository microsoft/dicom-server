// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    public sealed class QueryTagsExpiredEventArgs : EventArgs
    {
        public DicomDataset DicomDataset { get; set; }

        public IReadOnlyCollection<QueryTag> NewQueryTags { get; set; }
    }
}
