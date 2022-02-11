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
using Microsoft.Health.Dicom.Core.Extensions;
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
            var partitionKey = _dicomRequestContextAccessor.RequestContext.GetPartitionKey();

            try
            {
                string requestedTransferSyntax = _retrieveTransferSyntaxHandler.GetTransferSyntax(message.ResourceType, message.AcceptHeaders, out AcceptHeaderDescriptor acceptHeaderDescriptor);
                bool isOriginalTransferSyntaxRequested = DicomTransferSyntaxUids.IsOriginalTransferSyntaxRequested(requestedTransferSyntax);

                IEnumerable<VersionedInstanceIdentifier> retrieveInstances = await _instanceStore.GetInstancesWithProperties(
                    message.ResourceType, partitionKey, message.StudyInstanceUid, message.SeriesInstanceUid, message.SopInstanceUid, cancellationToken);

                if (!retrieveInstances.Any())
                {
                    throw new InstanceNotFoundException();
                }

                IEnumerable<RetrieveResourceInstance> resultInstances = await Task.WhenAll(
                    retrieveInstances.Select(async x =>
                    {
                        Stream s = await _blobDataStore.GetFileAsync(x, cancellationToken);
                        return new RetrieveResourceInstance(s, GetResponseTransferSyntax(requestedTransferSyntax, x));
                    }));

                long lengthOfResultStreams = resultInstances.Sum(i => i.Stream.Length);
                _dicomRequestContextAccessor.RequestContext.IsTranscodeRequested = !isOriginalTransferSyntaxRequested;
                _dicomRequestContextAccessor.RequestContext.BytesTranscoded = isOriginalTransferSyntaxRequested ? 0 : lengthOfResultStreams;

                if (message.ResourceType == ResourceType.Frames)
                {
                    RetrieveResourceInstance instanceStream = resultInstances.Single();
                    IReadOnlyCollection<Stream> frames = await _frameHandler.GetFramesResourceAsync(
                            instanceStream.Stream,
                            message.Frames,
                            isOriginalTransferSyntaxRequested,
                            requestedTransferSyntax);

                    return new RetrieveResourceResponse(
                        frames.Select(f => new RetrieveResourceInstance(f, instanceStream.TransferSyntaxUid)),
                        acceptHeaderDescriptor.MediaType);
                }
                else if (!isOriginalTransferSyntaxRequested)
                {
                    resultInstances = await Task.WhenAll(resultInstances.Select(async x => new RetrieveResourceInstance(await _transcoder.TranscodeFileAsync(x.Stream, requestedTransferSyntax), x.TransferSyntaxUid)));
                }

                return new RetrieveResourceResponse(resultInstances, acceptHeaderDescriptor.MediaType);
            }
            catch (DataStoreException e)
            {
                // Log request details associated with exception. Note that the details are not for the store call that failed but for the request only.
                _logger.LogError(e, "Error retrieving dicom resource. StudyInstanceUid: {StudyInstanceUid} SeriesInstanceUid: {SeriesInstanceUid} SopInstanceUid: {SopInstanceUid}", message.StudyInstanceUid, message.SeriesInstanceUid, message.SopInstanceUid);

                throw;
            }
        }

        private static string GetResponseTransferSyntax(string requestedTransferSyntax, VersionedInstanceIdentifier identifier)
        {
            bool isOriginalTransferSyntaxRequested = DicomTransferSyntaxUids.IsOriginalTransferSyntaxRequested(requestedTransferSyntax);
            if (isOriginalTransferSyntaxRequested)
            {
                return GetOriginalTransferSyntaxWithBackCompat(requestedTransferSyntax, identifier);
            }
            return requestedTransferSyntax;
        }

        /// <summary>
        /// Existing dicom files(as of Feb 2022) do not have transferSyntax stored. 
        /// Untill we backfill those files, we need this existing buggy fall back code: requestedTransferSyntax can be "*" which is the wrong content-type to return
        /// </summary>
        /// <param name="requestedTransferSyntax"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private static string GetOriginalTransferSyntaxWithBackCompat(string requestedTransferSyntax, VersionedInstanceIdentifier identifier)
        {
            return string.IsNullOrEmpty(identifier.Properties?.TransferSyntaxUid) ? requestedTransferSyntax : identifier.Properties.TransferSyntaxUid;
        }
    }
}
