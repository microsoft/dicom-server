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
                string dicomUri = string.Empty;
                string fhirUri = string.Empty;

                if (row.Table.Columns.Contains("URI"))
                {
                    dicomUri = row["URI"]?.ToString();
                }
                else
                {
                    continue;
                }

                if (row.Table.Columns.Contains("fullUrl"))
                {
                    fhirUri = row["fullUrl"].ToString();
                }
                else
                {
                    continue;
                }

                var dicomResource = new CohortResource();
                dicomResource.ReferenceUrl = dicomUri;
                dicomResource.ResourceType = CohortResourceType.DICOM;
                dicomResource.ResourceId = GetDicomResourceId(dicomUri);

                var fhirResource = new CohortResource();
                fhirResource.ReferenceUrl = fhirUri;
                fhirResource.ResourceType = CohortResourceType.FHIR;
                fhirResource.ResourceId = GetFhirResourceId(fhirUri);

                resources.Add(dicomResource);
                resources.Add(fhirResource);
            }

            cohortData.SearchText = searchQueryText;
            cohortData.CohortResources = resources;

            await _cohortQueryStore.AddCohortResources(cohortData, new System.Threading.CancellationToken()).ConfigureAwait(false);
            return cohortData;
        }

        private static CohortResourceType FindUriType(string uri)
        {
            if (uri.Contains("Patient", StringComparison.OrdinalIgnoreCase))
            {
                return CohortResourceType.FHIR;
            }

            if (uri.Contains("studies", StringComparison.OrdinalIgnoreCase) &&
                uri.Contains("series", StringComparison.OrdinalIgnoreCase) &&
                uri.Contains("instances", StringComparison.OrdinalIgnoreCase))
            {
                return CohortResourceType.DICOM;
            }

            return CohortResourceType.DICOM;
        }

        private static string GetFhirResourceId(string uri)
        {
            var splitUri = uri.Split('/');
            bool takeNextPiece = false;

            foreach (string piece in splitUri)
            {
                if (takeNextPiece)
                {
                    return piece;
                }
                else if (piece.Equals("Patient", StringComparison.OrdinalIgnoreCase))
                {
                    takeNextPiece = true;
                }
            }

            return string.Empty;
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
