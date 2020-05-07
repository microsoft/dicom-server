// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Query.Model
{
    public class QueryResult
    {
        public QueryResult(IEnumerable<VersionedInstanceIdentifier> entries)
        {
            EnsureArg.IsNotNull(entries, nameof(entries));
            DicomInstances = entries;
        }

        public IEnumerable<VersionedInstanceIdentifier> DicomInstances { get; }
    }
}
