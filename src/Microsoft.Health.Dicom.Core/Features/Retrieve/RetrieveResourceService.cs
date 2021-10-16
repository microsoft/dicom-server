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
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class RetrieveResourceService : IRetrieveResourceService
    {
        private readonly IFileStore _blobDataStore;
        private readonly IInstanceStore _instanceStore;
        private readonly ITranscoder _transcoder;
        private readonly IFrameHandler _frameHandler;
        private readonly IRetrieveTransferSyntaxHandler _retrieveTransferSyntaxHandler;
        private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
        private readonly ILogger<RetrieveResourceService> _logger;

        public RetrieveResourceService(
            IInstanceStore instanceStore,
            IFileStore blobDataStore,
            ITranscoder transcoder,
            IFrameHandler frameHandler,
            IRetrieveTransferSyntaxHandler retrieveTransferSyntaxHandler,
            IDicomRequestContextAccessor dicomRequestContextAccessor,
            ILogger<RetrieveResourceService> logger)
        {
            EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
            EnsureArg.IsNotNull(blobDataStore, nameof(blobDataStore));
            EnsureArg.IsNotNull(transcoder, nameof(transcoder));
            EnsureArg.IsNotNull(frameHandler, nameof(frameHandler));
            EnsureArg.IsNotNull(retrieveTransferSyntaxHandler, nameof(retrieveTransferSyntaxHandler));
            EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _instanceStore = instanceStore;
            _blobDataStore = blobDataStore;
            _transcoder = transcoder;
            _frameHandler = frameHandler;
            _retrieveTransferSyntaxHandler = retrieveTransferSyntaxHandler;
            _dicomRequestContextAccessor = dicomRequestContextAccessor;
            _logger = logger;
        }

        public async Task<RetrieveResourceResponse> GetInstanceResourceAsync(RetrieveResourceRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            try
            {
                string transferSyntax = _retrieveTransferSyntaxHandler.GetTransferSyntax(message.ResourceType, message.AcceptHeaders, out AcceptHeaderDescriptor acceptHeaderDescriptor);
                bool isOriginalTransferSyntaxRequested = DicomTransferSyntaxUids.IsOriginalTransferSyntaxRequested(transferSyntax);

                IEnumerable<VersionedInstanceIdentifier> retrieveInstances = await _instanceStore.GetInstancesToRetrieve(
                    message.ResourceType, message.StudyInstanceUid, message.SeriesInstanceUid, message.SopInstanceUid, cancellationToken);

                if (!retrieveInstances.Any())
                {
                    throw new InstanceNotFoundException();
                }

                Stream[] resultStreams = await Task.WhenAll(
                    retrieveInstances.Select(x => _blobDataStore.GetFileAsync(x, cancellationToken)));

                long lengthOfResultStreams = resultStreams.Sum(stream => stream.Length);
                _dicomRequestContextAccessor.RequestContext.IsTranscodeRequested = !isOriginalTransferSyntaxRequested;
                _dicomRequestContextAccessor.RequestContext.BytesTranscoded = isOriginalTransferSyntaxRequested ? 0 : lengthOfResultStreams;

                if (message.ResourceType == ResourceType.Frames)
                {
                    return new RetrieveResourceResponse(
                        await _frameHandler.GetFramesResourceAsync(
                        resultStreams.Single(), message.Frames, isOriginalTransferSyntaxRequested, transferSyntax),
                        acceptHeaderDescriptor.MediaType,
                        transferSyntax);
                }
                else
                {
                    if (!isOriginalTransferSyntaxRequested)
                    {
                        resultStreams = await Task.WhenAll(resultStreams.Select(x => _transcoder.TranscodeFileAsync(x, transferSyntax)));
                    }

                    resultStreams = resultStreams.Select(stream =>
                        new LazyTransformReadOnlyStream<Stream>(
                            stream,
                            s => ResetDicomFileStream(s))).ToArray();
                }

                return new RetrieveResourceResponse(resultStreams, acceptHeaderDescriptor.MediaType, transferSyntax);
            }
            catch (DataStoreException e)
            {
                // Log request details associated with exception. Note that the details are not for the store call that failed but for the request only.
                _logger.LogError(e, "Error retrieving dicom resource. StudyInstanceUid: {StudyInstanceUid} SeriesInstanceUid: {SeriesInstanceUid} SopInstanceUid: {SopInstanceUid}", message.StudyInstanceUid, message.SeriesInstanceUid, message.SopInstanceUid);

                throw;
            }
        }

        private static Stream ResetDicomFileStream(Stream stream)
        {
            stream.Seek(offset: 0, SeekOrigin.Begin);
            return stream;
        }
    }
}
