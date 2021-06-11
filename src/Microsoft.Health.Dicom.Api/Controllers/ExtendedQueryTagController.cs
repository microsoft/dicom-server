// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0-prerelease")]
    [ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
    public class ExtendedQueryTagController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ExtendedQueryTagController> _logger;
        private readonly bool _featureEnabled;

        public ExtendedQueryTagController(IMediator mediator, ILogger<ExtendedQueryTagController> logger, IOptions<FeatureConfiguration> featureConfiguration)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));

            _mediator = mediator;
            _logger = logger;
            _featureEnabled = featureConfiguration.Value.EnableExtendedQueryTags;
        }

        [BodyModelStateValidator]
        [ProducesResponseType(typeof(JsonResult), (int)HttpStatusCode.Accepted)]
        [HttpPost]
        [Route(KnownRoutes.VersionedExtendedQueryTagRoute)]
        [Route(KnownRoutes.ExtendedQueryTagRoute)]
        [AuditEventType(AuditEventSubType.AddExtendedQueryTag)]
        public async Task<IActionResult> PostAsync([FromBody] IEnumerable<AddExtendedQueryTagEntry> extendedQueryTags)
        {
            _logger.LogInformation("DICOM Web Add Extended Query Tag request received, with extendedQueryTags {extendedQueryTags}.", extendedQueryTags);

            EnsureFeatureIsEnabled();
            AddExtendedQueryTagResponse response = await _mediator.AddExtendedQueryTagsAsync(extendedQueryTags, HttpContext.RequestAborted);

            return StatusCode(
               (int)HttpStatusCode.Accepted, response);
        }

        [ProducesResponseType(typeof(JsonResult), (int)HttpStatusCode.NoContent)]
        [HttpDelete]
        [Route(KnownRoutes.VersionedDeleteExtendedQueryTagRoute)]
        [Route(KnownRoutes.DeleteExtendedQueryTagRoute)]
        [AuditEventType(AuditEventSubType.RemoveExtendedQueryTag)]
        public async Task<IActionResult> DeleteAsync(string tagPath)
        {
            _logger.LogInformation("DICOM Web Delete Extended Query Tag request received, with extended query tag path {tagPath}.", tagPath);

            EnsureFeatureIsEnabled();
            DeleteExtendedQueryTagResponse response = await _mediator.DeleteExtendedQueryTagAsync(tagPath, HttpContext.RequestAborted);

            return StatusCode((int)HttpStatusCode.NoContent, response);
        }

        /// <summary>
        /// Handles requests to get all extended query tags.
        /// </summary>
        /// <returns>
        /// Returns Bad Request if given path can't be parsed. Returns Not Found if given path doesn't map to a stored
        /// extended query tag or if no extended query tags are stored. Returns OK with a JSON body of all tags in other cases.
        /// </returns>
        [ProducesResponseType(typeof(JsonResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet]
        [Route(KnownRoutes.VersionedExtendedQueryTagRoute)]
        [Route(KnownRoutes.ExtendedQueryTagRoute)]
        [AuditEventType(AuditEventSubType.GetAllExtendedQueryTags)]
        public async Task<IActionResult> GetAllTagsAsync()
        {
            _logger.LogInformation("DICOM Web Get Extended Query Tag request received for all extended query tags");

            EnsureFeatureIsEnabled();
            GetAllExtendedQueryTagsResponse response = await _mediator.GetAllExtendedQueryTagsAsync(HttpContext.RequestAborted);

            return StatusCode(
                (int)HttpStatusCode.OK, response.ExtendedQueryTags);
        }

        /// <summary>
        /// Handles requests to get individual extended query tags.
        /// </summary>
        /// <param name="tagPath">Path for requested extended query tag.</param>
        /// <returns>
        /// Returns Bad Request if given path can't be parsed. Returns Not Found if given path doesn't map to a stored
        /// extended query tag. Returns OK with a JSON body of requested tag in other cases.
        /// </returns>
        [ProducesResponseType(typeof(JsonResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet]
        [Route(KnownRoutes.VersionedGetExtendedQueryTagRoute)]
        [Route(KnownRoutes.GetExtendedQueryTagRoute)]
        [AuditEventType(AuditEventSubType.GetExtendedQueryTag)]
        public async Task<IActionResult> GetTagAsync(string tagPath)
        {
            _logger.LogInformation("DICOM Web Get Extended Query Tag request received for extended query tag: {tagPath}");

            EnsureFeatureIsEnabled();
            GetExtendedQueryTagResponse response = await _mediator.GetExtendedQueryTagAsync(tagPath, HttpContext.RequestAborted);

            return StatusCode(
                (int)HttpStatusCode.OK, response.ExtendedQueryTag);
        }

        private void EnsureFeatureIsEnabled()
        {
            if (!_featureEnabled)
            {
                throw new ExtendedQueryTagFeatureDisabledException();
            }
        }
    }
}
