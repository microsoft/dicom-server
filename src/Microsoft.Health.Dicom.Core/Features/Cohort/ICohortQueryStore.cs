// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Cohort
{
    public interface ICohortQueryStore
    {
        Task AddCohortResources(CohortData cohortData, CancellationToken cancellationToken);

        Task<CohortData> GetCohortResources(Guid cohortId, CancellationToken cancellationToken);
    }
}
