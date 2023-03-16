// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using SixLabors.ImageSharp;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;
public class RetrieveRenderedService : IRetrieveRenderedService
{
    private readonly IFileStore _blobDataStore;
    private readonly IInstanceStore _instanceStore;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly ILogger<RetrieveResourceService> _logger;

    public RetrieveRenderedService(
        IInstanceStore instanceStore,
        IFileStore blobDataStore,
        IDicomRequestContextAccessor dicomRequestContextAccessor,
        ILogger<RetrieveResourceService> logger)
    {
        EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
        EnsureArg.IsNotNull(blobDataStore, nameof(blobDataStore));
        EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _instanceStore = instanceStore;
        _blobDataStore = blobDataStore;
        _dicomRequestContextAccessor = dicomRequestContextAccessor;
        _logger = logger;
    }

    public async Task<RetrieveRenderedResponse> RetrieveSopInstanceRenderedAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(studyInstanceUid, nameof(studyInstanceUid));
        EnsureArg.IsNotNull(seriesInstanceUid, nameof(seriesInstanceUid));
        EnsureArg.IsNotNull(sopInstanceUid, nameof(sopInstanceUid));
        var partitionKey = _dicomRequestContextAccessor.RequestContext.GetPartitionKey();

        try
        {
            // this call throws NotFound when zero instance found
            IEnumerable<InstanceMetadata> retrieveInstances = await _instanceStore.GetInstancesWithProperties(
                ResourceType.Instance, partitionKey, studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken);

            InstanceMetadata instance = retrieveInstances.First();

            _dicomRequestContextAccessor.RequestContext.PartCount = retrieveInstances.Count();

            Stream stream = await _blobDataStore.GetFileAsync(instance.VersionedInstanceIdentifier, cancellationToken);

            DicomFile dicomFile = await DicomFile.OpenAsync(stream, FileReadOption.ReadLargeOnDemand);

            var dicomImage = new DicomImage(dicomFile.Dataset);
            using var img = dicomImage.RenderImage();
            using var sharpImage = img.AsSharpImage();
            Stream imageFormat = new MemoryStream();
            sharpImage.SaveAsJpeg(imageFormat, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
            imageFormat.Position = 0;

            return new RetrieveRenderedResponse(new RetrieveResourceInstance(imageFormat, streamLength: imageFormat.Length), "image/jpeg");
        }
        catch (DataStoreException e)
        {
            // Log request details associated with exception. Note that the details are not for the store call that failed but for the request only.
            _logger.LogError(e, "Error retrieving dicom resource to render. StudyInstanceUid: {StudyInstanceUid} SeriesInstanceUid: {SeriesInstanceUid} SopInstanceUid: {SopInstanceUid}", studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            throw;
        }
    }
}
