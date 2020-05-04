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
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class DicomRetrieveResourceService : IDicomRetrieveResourceService
    {
        private readonly IDicomFileStore _dicomBlobDataStore;
        private readonly IDicomInstanceStore _dicomInstanceStore;
        private readonly IDicomRetrieveTranscoder _dicomRetrieveTranscoder;
        private readonly IDicomFrameHandler _dicomFrameHandler;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly ILogger<DicomRetrieveResourceService> _logger;

        public DicomRetrieveResourceService(
            IDicomInstanceStore dicomInstanceStore,
            IDicomFileStore dicomBlobDataStore,
            IDicomRetrieveTranscoder dicomRetrieveTranscoder,
            IDicomFrameHandler dicomFrameHandler,
            RecyclableMemoryStreamManager recyclableMemoryStreamManager,
            ILogger<DicomRetrieveResourceService> logger)
        {
            EnsureArg.IsNotNull(dicomInstanceStore, nameof(dicomInstanceStore));
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
            EnsureArg.IsNotNull(logger, nameof(logger));
            _dicomInstanceStore = dicomInstanceStore;
            _dicomBlobDataStore = dicomBlobDataStore;
            _dicomRetrieveTranscoder = dicomRetrieveTranscoder;
            _dicomFrameHandler = dicomFrameHandler;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
            _logger = logger;
        }

        // TODO change the input output params and setting the status code. US #73197
        public async Task<DicomRetrieveResourceResponse> GetInstanceResourceAsync(DicomRetrieveResourceRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            try
            {
                IEnumerable<VersionedDicomInstanceIdentifier> retrieveInstances = await _dicomInstanceStore.GetInstancesToRetrieve(
                    message.ResourceType, message.StudyInstanceUid, message.SeriesInstanceUid, message.SopInstanceUid, cancellationToken);

                if (!retrieveInstances.Any())
                {
                    throw new DicomInstanceNotFoundException();
                }

                Stream[] resultStreams = await Task.WhenAll(
                    retrieveInstances.Select(x => _dicomBlobDataStore.GetFileAsync(x, cancellationToken)));

                bool isPartialSuccess = false;

                if (message.ResourceType == ResourceType.Frames)
                {
                    resultStreams = _dicomFrameHandler.GetFramesResourceAsync(
                        resultStreams.Single(), message.Frames, message.OriginalTransferSyntaxRequested(), message.RequestedRepresentation).Result;
                }
                else
                {
                    if (!message.OriginalTransferSyntaxRequested())
                    {
                        (isPartialSuccess, resultStreams) = _dicomRetrieveTranscoder.TranscodeDicomFiles(resultStreams, message.RequestedRepresentation);
                    }

                    resultStreams = resultStreams.Select(stream =>
                        new LazyTransformReadOnlyStream<Stream>(
                            stream,
                            s => ResetDicomFileStream(s))).ToArray();
                }

                return new DicomRetrieveResourceResponse(isPartialSuccess, resultStreams);
            }
            catch (DicomDataStoreException e)
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
