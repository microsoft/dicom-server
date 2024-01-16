// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Conventions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Messages.Store;
using Microsoft.Health.Dicom.Core.Messages.Update;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Update;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Operations;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers;

[QueryModelStateValidator]
[ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
[ServiceFilter(typeof(PopulateDataPartitionFilterAttribute))]
public class StoreController : ControllerBase
{
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly IMediator _mediator;
    private readonly ILogger<StoreController> _logger;
    private readonly bool _asyncOperationDisabled;

    public StoreController(IMediator mediator, ILogger<StoreController> logger, IOptions<FeatureConfiguration> featureConfiguration, IDicomRequestContextAccessor dicomRequestContextAccessor)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));
        EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));

        _mediator = mediator;
        _logger = logger;
        _asyncOperationDisabled = featureConfiguration.Value.DisableOperations;
        _dicomRequestContextAccessor = dicomRequestContextAccessor;
    }

    [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson })]
    [Produces(KnownContentTypes.ApplicationDicomJson)]
    [Consumes(KnownContentTypes.ApplicationDicom, KnownContentTypes.MultipartRelated)]
    [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
    [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.Conflict)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.UnsupportedMediaType)]
    [HttpPost]
    [VersionedPartitionRoute(KnownRoutes.StoreInstancesRoute, Name = KnownRouteNames.PartitionStoreInstance)]
    [VersionedRoute(KnownRoutes.StoreInstancesRoute, Name = KnownRouteNames.StoreInstance)]
    [AuditEventType(AuditEventSubType.Store)]
    public async Task<IActionResult> PostInstanceAsync()
    {
        return await PostAsync(null);
    }

    [AcceptContentFilter(new[] { KnownContentTypes.ApplicationDicomJson })]
    [Produces(KnownContentTypes.ApplicationDicomJson)]
    [Consumes(KnownContentTypes.ApplicationDicom, KnownContentTypes.MultipartRelated)]
    [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
    [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.Conflict)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.UnsupportedMediaType)]
    [HttpPost]
    [VersionedPartitionRoute(KnownRoutes.StoreInstancesInStudyRoute, Name = KnownRouteNames.PartitionStoreInstancesInStudy)]
    [VersionedRoute(KnownRoutes.StoreInstancesInStudyRoute, Name = KnownRouteNames.StoreInstancesInStudy)]
    [AuditEventType(AuditEventSubType.Store)]
    public async Task<IActionResult> PostInstanceInStudyAsync(string studyInstanceUid)
    {
        return await PostAsync(studyInstanceUid);
    }

    [HttpPost]
    [IntroducedInApiVersion(2)]
    [Consumes(KnownContentTypes.ApplicationJson)]
    [ProducesResponseType(typeof(OperationReference), (int)HttpStatusCode.Accepted)]
    [ProducesResponseType(typeof(DicomDataset), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [VersionedPartitionRoute(KnownRoutes.UpdateInstanceRoute, Name = KnownRouteNames.PartitionedUpdateInstance)]
    [VersionedRoute(KnownRoutes.UpdateInstanceRoute, Name = KnownRouteNames.UpdateInstance)]
    [AuditEventType(AuditEventSubType.UpdateStudy)]
    public async Task<IActionResult> UpdateAsync([FromBody][Required] UpdateSpecification updateSpecification)
    {
        if (_asyncOperationDisabled)
        {
            throw new DicomAsyncOperationDisabledException();
        }

        // DICOM update only supported in API version 2 and above
        if (_dicomRequestContextAccessor.RequestContext.Version < ApiVersionsConvention.MinimumSupportedVersionForDicomUpdate)
        {
            return StatusCode((int)HttpStatusCode.NotFound);
        }

        UpdateInstanceResponse response = await _mediator.UpdateInstanceAsync(updateSpecification);
        if (response.FailedDataset != null)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, response.FailedDataset);
        }
        return StatusCode((int)HttpStatusCode.Accepted, response.Operation);
    }

    private async Task<IActionResult> PostAsync(string studyInstanceUid)
    {
        _logger.LogInformation("DICOM Web Store Transaction request received, with study instance UID {StudyInstanceUid}", studyInstanceUid);

        StoreResponse storeResponse = await _mediator.StoreDicomResourcesAsync(
            Request.Body,
            Request.ContentType,
            studyInstanceUid,
            HttpContext.RequestAborted);
        if (!string.IsNullOrEmpty(storeResponse.Warning))
        {
            Response.SetWarning(HttpWarningCode.MiscPersistentWarning, Request.GetHost(dicomStandards: true), storeResponse.Warning);
        }

        return StatusCode(
            (int)storeResponse.Status.ToHttpStatusCode(),
            storeResponse.Dataset);
    }
}
