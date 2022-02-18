// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
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
        private readonly RetrieveConfiguration _retrieveConfiguration;
        private readonly ILogger<RetrieveResourceService> _logger;

        public RetrieveResourceService(
            IInstanceStore instanceStore,
            IFileStore blobDataStore,
            ITranscoder transcoder,
            IFrameHandler frameHandler,
            IRetrieveTransferSyntaxHandler retrieveTransferSyntaxHandler,
            IDicomRequestContextAccessor dicomRequestContextAccessor,
            IOptionsSnapshot<RetrieveConfiguration> retrieveConfiguration,
            ILogger<RetrieveResourceService> logger)
        {
            EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
            EnsureArg.IsNotNull(blobDataStore, nameof(blobDataStore));
            EnsureArg.IsNotNull(transcoder, nameof(transcoder));
            EnsureArg.IsNotNull(frameHandler, nameof(frameHandler));
            EnsureArg.IsNotNull(retrieveTransferSyntaxHandler, nameof(retrieveTransferSyntaxHandler));
            EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(retrieveConfiguration?.Value, nameof(retrieveConfiguration));

            _instanceStore = instanceStore;
            _blobDataStore = blobDataStore;
            _transcoder = transcoder;
            _frameHandler = frameHandler;
            _retrieveTransferSyntaxHandler = retrieveTransferSyntaxHandler;
            _dicomRequestContextAccessor = dicomRequestContextAccessor;
            _retrieveConfiguration = retrieveConfiguration?.Value;
            _logger = logger;
        }

        public async Task<RetrieveResourceResponse> GetInstanceResourceAsync(RetrieveResourceRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));
            var partitionKey = _dicomRequestContextAccessor.RequestContext.GetPartitionKey();

            try
            {
                string requestedTransferSyntax = _retrieveTransferSyntaxHandler.GetTransferSyntax(message.ResourceType, message.AcceptHeaders, out AcceptHeaderDescriptor acceptHeaderDescriptor, out AcceptHeader acceptedHeader);
                bool isOriginalTransferSyntaxRequested = DicomTransferSyntaxUids.IsOriginalTransferSyntaxRequested(requestedTransferSyntax);

                // this call throws NotFound when zero instance found
                IEnumerable<InstanceMetadata> retrieveInstances = await _instanceStore.GetInstancesWithProperties(
                    message.ResourceType, partitionKey, message.StudyInstanceUid, message.SeriesInstanceUid, message.SopInstanceUid, cancellationToken);

                // we will only support retrieving multiple instance if requested in original format, since we can do lazyStreams
                if (retrieveInstances.Count() > 1 && !isOriginalTransferSyntaxRequested)
                {
                    throw new NotAcceptableException(
                        string.Format(DicomCoreResource.RetrieveServiceMultiInstanceTranscodingNotSupported, requestedTransferSyntax));
                }

                // if single instance, check if we can support transcoding/frame parsing based on fileSize
                if (retrieveInstances.Count() == 1)
                {
                    InstanceMetadata instance = retrieveInstances.First();
                    var needsTranscoding = NeedsTranscoding(isOriginalTransferSyntaxRequested, requestedTransferSyntax, instance);

                    // need a realized stream if the stream needs to be parsed into a DicomFile
                    if (needsTranscoding || message.ResourceType == ResourceType.Frames)
                    {
                        FileProperties fileProperties = await _blobDataStore.GetFilePropertiesAsync(instance.VersionedInstanceIdentifier, cancellationToken);

                        // limit the file size that can be read in memory
                        if (fileProperties.ContentLength > _retrieveConfiguration.MaxDicomFileSize)
                        {
                            throw new NotAcceptableException(string.Format(DicomCoreResource.RetrieveServiceFileTooBig, _retrieveConfiguration.MaxDicomFileSize));
                        }

                        // set properties used for billing
                        if (needsTranscoding)
                        {
                            _dicomRequestContextAccessor.RequestContext.IsTranscodeRequested = true;
                            _dicomRequestContextAccessor.RequestContext.BytesTranscoded = fileProperties.ContentLength;
                        }

                        Stream stream = await _blobDataStore.GetFileAsync(instance.VersionedInstanceIdentifier, cancellationToken);

                        if (message.ResourceType == ResourceType.Frames)
                        {
                            // eagerly doing getFrames to validate frame numbers are valid before returning a response
                            IReadOnlyCollection<Stream> frameStreams = await _frameHandler.GetFramesResourceAsync(
                                stream,
                                message.Frames,
                                isOriginalTransferSyntaxRequested,
                                requestedTransferSyntax);

                            _dicomRequestContextAccessor.RequestContext.BytesTranscoded = frameStreams.Sum(f => f.Length);

                            IAsyncEnumerable<RetrieveResourceInstance> frames = GetAsyncEnumerableFrameStreams(
                                frameStreams,
                                instance,
                                isOriginalTransferSyntaxRequested,
                                requestedTransferSyntax);

                            return new RetrieveResourceResponse(
                                frames,
                                acceptHeaderDescriptor.MediaType,
                                acceptedHeader.IsSinglePart);
                        }

                        if (needsTranscoding)
                        {
                            IAsyncEnumerable<RetrieveResourceInstance> transcodedStream = GetAsyncEnumerableTranscodedStreams(
                                isOriginalTransferSyntaxRequested,
                                stream,
                                instance,
                                requestedTransferSyntax);

                            return new RetrieveResourceResponse(
                                transcodedStream,
                                acceptHeaderDescriptor.MediaType,
                                acceptedHeader.IsSinglePart);
                        }
                    }
                }

                IAsyncEnumerable<RetrieveResourceInstance> responses = GetAsyncEnumerableStreams(retrieveInstances, isOriginalTransferSyntaxRequested, requestedTransferSyntax, cancellationToken);
                return new RetrieveResourceResponse(responses, acceptHeaderDescriptor.MediaType, acceptedHeader.IsSinglePart);
            }
            catch (DataStoreException e)
            {
                // Log request details associated with exception. Note that the details are not for the store call that failed but for the request only.
                _logger.LogError(e, "Error retrieving dicom resource. StudyInstanceUid: {StudyInstanceUid} SeriesInstanceUid: {SeriesInstanceUid} SopInstanceUid: {SopInstanceUid}", message.StudyInstanceUid, message.SeriesInstanceUid, message.SopInstanceUid);

                throw;
            }
        }

        private static string GetResponseTransferSyntax(bool isOriginalTransferSyntaxRequested, string requestedTransferSyntax, InstanceMetadata instanceMetadata)
        {
            if (isOriginalTransferSyntaxRequested)
            {
                return GetOriginalTransferSyntaxWithBackCompat(requestedTransferSyntax, instanceMetadata);
            }
            return requestedTransferSyntax;
        }

        /// <summary>
        /// Existing dicom files(as of Feb 2022) do not have transferSyntax stored. 
        /// Untill we backfill those files, we need this existing buggy fall back code: requestedTransferSyntax can be "*" which is the wrong content-type to return
        /// </summary>
        /// <param name="requestedTransferSyntax"></param>
        /// <param name="instanceMetadata"></param>
        /// <returns></returns>
        private static string GetOriginalTransferSyntaxWithBackCompat(string requestedTransferSyntax, InstanceMetadata instanceMetadata)
        {
            return string.IsNullOrEmpty(instanceMetadata.InstanceProperties.TransferSyntaxUid) ? requestedTransferSyntax : instanceMetadata.InstanceProperties.TransferSyntaxUid;
        }

        private static bool NeedsTranscoding(bool isOriginalTransferSyntaxRequested, string requestedTransferSyntax, InstanceMetadata instanceMetadata)
        {
            if (isOriginalTransferSyntaxRequested)
                return false;

            return !(instanceMetadata.InstanceProperties.TransferSyntaxUid != null
                    && DicomTransferSyntaxUids.AreEqual(requestedTransferSyntax, instanceMetadata.InstanceProperties.TransferSyntaxUid));
        }

        private async IAsyncEnumerable<RetrieveResourceInstance> GetAsyncEnumerableStreams(
            IEnumerable<InstanceMetadata> instanceMetadatas,
            bool isOriginalTransferSyntaxRequested,
            string requestedTransferSyntax,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var instanceMetadata in instanceMetadatas)
            {
                yield return
                    new RetrieveResourceInstance(
                        await _blobDataStore.GetFileAsync(instanceMetadata.VersionedInstanceIdentifier, cancellationToken),
                        GetResponseTransferSyntax(isOriginalTransferSyntaxRequested, requestedTransferSyntax, instanceMetadata));
            }
        }

        private static async IAsyncEnumerable<RetrieveResourceInstance> GetAsyncEnumerableFrameStreams(
            IEnumerable<Stream> frameStreams,
            InstanceMetadata instanceMetadata,
            bool isOriginalTransferSyntaxRequested,
            string requestedTransferSyntax)
        {
            // fake await to return AsyncEnumerable and keep the response consistent
            await Task.Run(() => 1);
            // responseTransferSyntax is same for all frames in a instance
            var responseTransferSyntax = GetResponseTransferSyntax(isOriginalTransferSyntaxRequested, requestedTransferSyntax, instanceMetadata);
            foreach (Stream frameStream in frameStreams)
            {
                yield return
                    new RetrieveResourceInstance(frameStream, responseTransferSyntax);
            }
        }

        private async IAsyncEnumerable<RetrieveResourceInstance> GetAsyncEnumerableTranscodedStreams(
            bool isOriginalTransferSyntaxRequested,
            Stream stream,
            InstanceMetadata instanceMetadata,
            string requestedTransferSyntax)
        {
            Stream transcodedStream = await _transcoder.TranscodeFileAsync(stream, requestedTransferSyntax);

            yield return new RetrieveResourceInstance(transcodedStream, GetResponseTransferSyntax(isOriginalTransferSyntaxRequested, requestedTransferSyntax, instanceMetadata));
        }

    }
}
