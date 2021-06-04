// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    /// <summary>
    /// External representation of a extended query tag entry for add.
    /// </summary>
    public class Operation
    {
        public string OperationId { get; set; }

        public IReadOnlyCollection<QueryTag> QueryTags { get; set; }
    }
}
