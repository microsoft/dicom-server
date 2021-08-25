// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Core.Models
{
    public class CohortData
    {
        public Guid CohortId { get; set; }

        public string SearchText { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "<Pending>")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<CohortResource> CohortResources { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}
