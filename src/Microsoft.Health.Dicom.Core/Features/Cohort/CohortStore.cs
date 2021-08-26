// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
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
            var cohortData = new CohortData();
            cohortData.CohortId = Guid.NewGuid();
            var resources = new List<CohortResource>();

            var searchResultTable = _aDXService.ExecuteQueryAsync(searchQueryText);

            foreach (DataRow row in searchResultTable.Rows)
            {
                string uri = row["URI"].ToString();

                // TODO: handle FHIR data
                var resource = new CohortResource();
                resource.ReferenceUrl = uri;
                resource.ResourceId = GetDicomResourceId(uri);
                resource.ResourceType = CohortResourceType.DICOM;

                resources.Add(resource);
            }

            cohortData.SearchText = searchQueryText;
            cohortData.CohortResources = resources;

            await _cohortQueryStore.AddCohortResources(cohortData, new System.Threading.CancellationToken()).ConfigureAwait(false);
            return cohortData;
        }

        private static string GetDicomResourceId(string uri)
        {
            var splitUri = uri.Split('/');
            List<string> resourceIdPieces = new List<string>();
            bool takeNextPiece = false;

            foreach (string piece in splitUri)
            {
                if (takeNextPiece)
                {
                    resourceIdPieces.Add(piece);
                    takeNextPiece = false;
                }
                else if (piece.Equals("studies", StringComparison.OrdinalIgnoreCase) ||
                    piece.Equals("series", StringComparison.OrdinalIgnoreCase) ||
                    piece.Equals("instances", StringComparison.OrdinalIgnoreCase))
                {
                    takeNextPiece = true;
                }
            }

            return string.Join('-', resourceIdPieces);
        }
    }
}
