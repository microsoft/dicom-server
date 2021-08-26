// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.ADX;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Cohort
{
    public class CohortStore : ICohortStore
    {
        private readonly ICohortQueryStore _cohortQueryStore;
        private readonly IADXService _aDXService;

        public CohortStore(ICohortQueryStore cohortQueryStore, IADXService aDXService)
        {
            _cohortQueryStore = cohortQueryStore;
            _aDXService = aDXService;
        }

        public async Task<CohortData> CreateCohortAsync(string searchQueryText)
        {
            // TODO: search using ADX
            // _aDXService.ExecuteQueryAsync(searchQueryText);
            // parse output for FHIR & DICOM data
            // create URLS from data

            var cohortData = new CohortData();
            cohortData.CohortId = Guid.NewGuid();
            cohortData.SearchText = searchQueryText;
            var resources = new List<CohortResource>();
            int i = 0;
            while (i < 10)
            {
                var resource = new CohortResource();
                resource.ResourceId = "resourceId" + i;
                resource.ResourceType = (i % 2) == 0 ? CohortResourceType.DICOM : CohortResourceType.FHIR;
                resource.ReferenceUrl = "url" + i;
                resources.Add(resource);
                i++;
            }
            cohortData.CohortResources = resources;

            await _cohortQueryStore.AddCohortResources(cohortData, new System.Threading.CancellationToken()).ConfigureAwait(false);
            cohortData = await _cohortQueryStore.GetCohortResources(cohortData.CohortId, new System.Threading.CancellationToken()).ConfigureAwait(false);
            return cohortData;
        }
    }
}
