// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public class WorkitemQueryResult
    {
        public WorkitemQueryResult(IEnumerable<WorkitemInstanceIdentifier> entries)
        {
            EnsureArg.IsNotNull(entries, nameof(entries));
            WorkitemInstances = entries;
        }

        public IEnumerable<WorkitemInstanceIdentifier> WorkitemInstances { get; }
    }
}
