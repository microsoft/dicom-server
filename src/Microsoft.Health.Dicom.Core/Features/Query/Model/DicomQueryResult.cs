// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class DicomQueryResult
    {
        public DicomQueryResult(IEnumerable<VersionedDicomInstanceIdentifier> entries)
        {
            EnsureArg.IsNotNull(entries, nameof(entries));
            DicomInstances = entries;
        }

        public IEnumerable<VersionedDicomInstanceIdentifier> DicomInstances { get; }
    }
}
