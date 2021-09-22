// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Web;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [ApiVersion("1.0-prerelease")]
    [ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
    public class ExtendedQueryTagController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ExtendedQueryTagController> _logger;
        private readonly bool _featureEnabled;

        public ExtendedQueryTagController(
            IMediator mediator,
            IOptions<FeatureConfiguration> featureConfiguration,
            ILogger<ExtendedQueryTagController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
            _featureEnabled = featureConfiguration.Value.EnableExtendedQueryTags;
        }

        [HttpPost]
        [BodyModelStateValidator]
        [Produces(KnownContentTypes.ApplicationJson)]
        [Consumes(KnownContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(AddExtendedQueryTagResponse), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [VersionedRoute(KnownRoutes.ExtendedQueryTagRoute)]
        [Route(KnownRoutes.ExtendedQueryTagRoute)]
        [AuditEventType(AuditEventSubType.AddExtendedQueryTag)]
        public async Task<IActionResult> PostAsync([Required][FromBody] IReadOnlyCollection<AddExtendedQueryTagEntry> extendedQueryTags)
        {
            _logger.LogInformation("DICOM Web Add Extended Query Tag request received, with extendedQueryTags {extendedQueryTags}.", extendedQueryTags);

            EnsureFeatureIsEnabled();
            AddExtendedQueryTagResponse response = await _mediator.AddExtendedQueryTagsAsync(extendedQueryTags, HttpContext.RequestAborted);

            Response.AddLocationHeader(response.Operation.Href);
            return StatusCode((int)HttpStatusCode.Accepted, response.Operation);
        }

        [Produces(KnownContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(DeleteExtendedQueryTagResponse), (int)HttpStatusCode.NoContent)]
        [HttpDelete]
        [VersionedRoute(KnownRoutes.DeleteExtendedQueryTagRoute)]
        [Route(KnownRoutes.DeleteExtendedQueryTagRoute)]
        [AuditEventType(AuditEventSubType.RemoveExtendedQueryTag)]
        public async Task<IActionResult> DeleteAsync(string tagPath)
        {
            _logger.LogInformation("DICOM Web Delete Extended Query Tag request received, with extended query tag path {tagPath}.", tagPath);

            EnsureFeatureIsEnabled();
            await _mediator.DeleteExtendedQueryTagAsync(tagPath, HttpContext.RequestAborted);

            return StatusCode((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Handles requests to get all extended query tags.
        /// </summary>
        /// <param name="limit">The maximum number of results to retrieve.</param>
        /// <param name="offset">The offset from which to retrieve paginated results.</param>
        /// <returns>
        /// Returns Bad Request if given path can't be parsed. Returns Not Found if given path doesn't map to a stored
        /// extended query tag or if no extended query tags are stored. Returns OK with a JSON body of all tags in other cases.
        /// </returns>
        [HttpGet]
        [Produces(KnownContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(IEnumerable<GetExtendedQueryTagEntry>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [VersionedRoute(KnownRoutes.ExtendedQueryTagRoute)]
        [Route(KnownRoutes.ExtendedQueryTagRoute)]
        [AuditEventType(AuditEventSubType.GetAllExtendedQueryTags)]
        public async Task<IActionResult> GetTagsAsync(
            [FromQuery, Range(1, 200)] int limit = 100,
            [FromQuery, Range(0, int.MaxValue)] int offset = 0)
        {
            // TODO: Enforce the above data annotations with ModelState.IsValid or use the [ApiController] attribute
            // for automatic error generation. However, we should change all errors across the API surface.
            _logger.LogInformation("DICOM Web Get Extended Query Tag request received for all extended query tags");

            EnsureFeatureIsEnabled();
            GetExtendedQueryTagsResponse response = await _mediator.GetExtendedQueryTagsAsync(
                limit,
                offset,
                HttpContext.RequestAborted);

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
        [HttpGet]
        [Produces(KnownContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(GetExtendedQueryTagEntry), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [VersionedRoute(KnownRoutes.GetExtendedQueryTagRoute, Name = KnownRouteNames.VersionedGetExtendedQueryTag)]
        [Route(KnownRoutes.GetExtendedQueryTagRoute, Name = KnownRouteNames.GetExtendedQueryTag)]
        [AuditEventType(AuditEventSubType.GetExtendedQueryTag)]
        public async Task<IActionResult> GetTagAsync(string tagPath)
        {
            _logger.LogInformation("DICOM Web Get Extended Query Tag request received for extended query tag: {tagPath}", tagPath);

            EnsureFeatureIsEnabled();
            GetExtendedQueryTagResponse response = await _mediator.GetExtendedQueryTagAsync(tagPath, HttpContext.RequestAborted);

            return StatusCode(
                (int)HttpStatusCode.OK, response.ExtendedQueryTag);
        }

        /// <summary>
        /// Handles requests to get extended query tag errors.
        /// </summary>
        /// <param name="tagPath">Path for requested extended query tag.</param>
        /// <param name="limit">The maximum number of results to retrieve.</param>
        /// <param name="offset">The offset from which to retrieve paginated results.</param>
        /// <returns>
        /// Returns Bad Request if given path can't be parsed. Returns Not Found if given path doesn't map to a stored
        /// error. Returns OK with a JSON body of requested tag error in other cases.
        /// </returns>
        [HttpGet]
        [Produces(KnownContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(IEnumerable<ExtendedQueryTagError>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [VersionedRoute(KnownRoutes.GetExtendedQueryTagErrorsRoute, Name = KnownRouteNames.VersionedGetExtendedQueryTagErrors)]
        [Route(KnownRoutes.GetExtendedQueryTagErrorsRoute, Name = KnownRouteNames.GetExtendedQueryTagErrors)]
        [AuditEventType(AuditEventSubType.GetExtendedQueryTagErrors)]
        public async Task<IActionResult> GetTagErrorsAsync(
            [FromRoute] string tagPath,
            [FromQuery, Range(1, 200)] int limit = 100,
            [FromQuery, Range(0, int.MaxValue)] int offset = 0)
        {
            _logger.LogInformation("DICOM Web Get Extended Query Tag Errors request received for extended query tag: {tagPath}", tagPath);

            EnsureFeatureIsEnabled();
            GetExtendedQueryTagErrorsResponse response = await _mediator.GetExtendedQueryTagErrorsAsync(tagPath, limit, offset, HttpContext.RequestAborted);

            return StatusCode((int)HttpStatusCode.OK, response.ExtendedQueryTagErrors);
        }

        [HttpPatch]
        [Produces(KnownContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(IEnumerable<ExtendedQueryTagError>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [VersionedRoute(KnownRoutes.UpdateExtendedQueryTagQueryStatusRoute, Name = KnownRouteNames.VersionedUpdateExtendedQueryTagQueryStatus)]
        [Route(KnownRoutes.UpdateExtendedQueryTagQueryStatusRoute, Name = KnownRouteNames.UpdateExtendedQueryTagQueryStatus)]
        [AuditEventType(AuditEventSubType.UpdateExtendedQueryTagQueryStatus)]
        public async Task<IActionResult> UpdateTagQueryStatusAsync([FromRoute] string tagPath, [Required][FromBody] UpdateExtendedQueryTagEntry updateEntry)
        {
            _logger.LogInformation("DICOM Web Update Extended Query Tag Query Status request received for extended query tag {tagPath} and new value {updateEntry}", tagPath, updateEntry);

            EnsureFeatureIsEnabled();
            var response = await _mediator.UpdateExtendedQueryTagAsync(tagPath, updateEntry, HttpContext.RequestAborted);

            return StatusCode((int)HttpStatusCode.OK, response.TagEntry);
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
