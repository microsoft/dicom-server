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
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class RetrieveResourceService : IRetrieveResourceService
    {
        private readonly IFileStore _blobDataStore;
        private readonly IInstanceStore _instanceStore;
        private readonly IRetrieveTranscoder _retrieveTranscoder;
        private readonly IFrameHandler _dicomFrameHandler;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly ILogger<RetrieveResourceService> _logger;

        public RetrieveResourceService(
            IInstanceStore instanceStore,
            IFileStore blobDataStore,
            IRetrieveTranscoder retrieveTranscoder,
            IFrameHandler dicomFrameHandler,
            RecyclableMemoryStreamManager recyclableMemoryStreamManager,
            ILogger<RetrieveResourceService> logger)
        {
            EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
            EnsureArg.IsNotNull(blobDataStore, nameof(blobDataStore));
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
            EnsureArg.IsNotNull(logger, nameof(logger));
            _instanceStore = instanceStore;
            _blobDataStore = blobDataStore;
            _retrieveTranscoder = retrieveTranscoder;
            _dicomFrameHandler = dicomFrameHandler;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
            _logger = logger;
        }

        // TODO change the input output params and setting the status code. US #73197
        public async Task<RetrieveResourceResponse> GetInstanceResourceAsync(RetrieveResourceRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            try
            {
                IEnumerable<VersionedInstanceIdentifier> retrieveInstances = await _instanceStore.GetInstancesToRetrieve(
                    message.ResourceType, message.StudyInstanceUid, message.SeriesInstanceUid, message.SopInstanceUid, cancellationToken);

                if (!retrieveInstances.Any())
                {
                    throw new InstanceNotFoundException();
                }

                Stream[] resultStreams = await Task.WhenAll(
                    retrieveInstances.Select(x => _blobDataStore.GetFileAsync(x, cancellationToken)));

                bool isPartialSuccess = false;

                if (message.ResourceType == ResourceType.Frames)
                {
                    resultStreams = await _dicomFrameHandler.GetFramesResourceAsync(
                        resultStreams.Single(), message.Frames, message.OriginalTransferSyntaxRequested(), message.RequestedRepresentation);
                }
                else
                {
                    if (!message.OriginalTransferSyntaxRequested())
                    {
                        (isPartialSuccess, resultStreams) = _retrieveTranscoder.TranscodeFiles(resultStreams, message.RequestedRepresentation);
                    }

                    resultStreams = resultStreams.Select(stream =>
                        new LazyTransformReadOnlyStream<Stream>(
                            stream,
                            s => ResetDicomFileStream(s))).ToArray();
                }

                return new RetrieveResourceResponse(isPartialSuccess, resultStreams);
            }
            catch (DataStoreException e)
            {
                // Log request details associated with exception. Note that the details are not for the store call that failed but for the request only.
                _logger.LogError(
                    e,
                    string.Format(
                        "Error retrieving dicom resource. StudyInstanceUid: {0} SeriesInstanceUid: {1} SopInstanceUid: {2}",
                        message.StudyInstanceUid,
                        message.SeriesInstanceUid,
                        message.SopInstanceUid));

                throw;
            }
        }

        private Stream ResetDicomFileStream(Stream stream)
        {
            stream.Seek(offset: 0, SeekOrigin.Begin);
            return stream;
        }
    }
}
