// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Text;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Cohort;
using Microsoft.Health.Dicom.Core.Models;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ApiVersion("1.0-prerelease")]
    [QueryModelStateValidator]
    [ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
    public class CohortController : Controller
    {
        private readonly ICohortStore _cohortStore;
        private readonly ILogger<CohortController> _logger;

        public CohortController(ICohortStore cohortStore, ILogger<CohortController> logger)
        {
            EnsureArg.IsNotNull(cohortStore, nameof(cohortStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _cohortStore = cohortStore;
            _logger = logger;
        }

        [HttpPost]
        [VersionedRoute(KnownRoutes.CohortRoute)]
        [Route(KnownRoutes.CohortRoute)]
        [AuditEventType(AuditEventSubType.Cohort)]
        public async Task<CohortData> CreateCohortAsync()
        {
            var searchQueryText = string.Empty;

            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                searchQueryText = await reader.ReadToEndAsync();
            }

            return await _cohortStore.CreateCohortAsync(searchQueryText);
        }
    }
}
