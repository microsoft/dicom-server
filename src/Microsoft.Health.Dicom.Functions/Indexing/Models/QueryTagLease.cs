// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Functions.Indexing.Models
{
    public class QueryTagLease
    {
        public IReadOnlyCollection<int> TagKeys { get; set; }

        public string OperationId { get; set; }
    }
}
