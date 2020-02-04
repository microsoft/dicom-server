// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    public class RetrieveDicomResourceHandler : BaseRetrieveDicomResourceHandler, IRequestHandler<RetrieveDicomResourceRequest, RetrieveDicomResourceResponse>
    {
        private static readonly DicomTransferSyntax DefaultTransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
        private readonly DicomDataStore _dicomDataStore;

        public RetrieveDicomResourceHandler(IDicomMetadataStore dicomMetadataStore, DicomDataStore dicomDataStore)
            : base(dicomMetadataStore)
        {
            EnsureArg.IsNotNull(dicomDataStore, nameof(dicomDataStore));

            _dicomDataStore = dicomDataStore;
        }

        public async Task<RetrieveDicomResourceResponse> Handle(
            RetrieveDicomResourceRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            DicomTransferSyntax parsedDicomTransferSyntax =
                message.OriginalTransferSyntaxRequested() ?
                null :
                string.IsNullOrWhiteSpace(message.RequestedRepresentation) ?
                    DefaultTransferSyntax :
                    DicomTransferSyntax.Parse(message.RequestedRepresentation);

            try
            {
                IEnumerable<DicomInstance> retrieveInstances = await GetInstancesToRetrieve(
                    message.ResourceType, message.StudyInstanceUID, message.SeriesInstanceUID, message.SopInstanceUID, cancellationToken);
                Stream[] resultStreams = await Task.WhenAll(retrieveInstances.Select(x => _dicomDataStore.GetDicomDataStreamAsync(x, cancellationToken)));

                var responseCode = HttpStatusCode.OK;

                if (message.ResourceType == ResourceType.Frames)
                {
                    // We first validate the file has the requested frames, then pass the frame for lazy encoding.
                    var dicomFile = DicomFile.Open(resultStreams.Single());
                    dicomFile.ValidateHasFrames(message.Frames);

                    if (!message.OriginalTransferSyntaxRequested() &&
                        !dicomFile.Dataset.CanTranscodeDataset(parsedDicomTransferSyntax))
                    {
                        throw new DataStoreException(HttpStatusCode.NotAcceptable);
                    }

                    resultStreams = message.Frames.Select(
                            x => new LazyTransformReadOnlyStream<DicomFile>(
                                dicomFile,
                                y => y.GetFrameAsDicomData(x, parsedDicomTransferSyntax)))
                        .ToArray();
                }
                else
                {
                    if (!message.OriginalTransferSyntaxRequested())
                    {
                        Stream[] filteredStreams = resultStreams.Where(x =>
                        {
                            var canTranscode = false;

                            try
                            {
                                // TODO: replace with FileReadOption.SkipLargeTags when updating to a future
                                // version of fo-dicom where https://github.com/fo-dicom/fo-dicom/issues/893 is fixed
                                var dicomFile = DicomFile.OpenAsync(x, FileReadOption.ReadLargeOnDemand).Result;
                                canTranscode = dicomFile.Dataset.CanTranscodeDataset(parsedDicomTransferSyntax);
                            }
                            catch (DicomFileException)
                            {
                                canTranscode = false;
                            }

                            x.Seek(0, SeekOrigin.Begin);

                            // If some of the instances are not transcodeable, Partial Content should be returned
                            if (!canTranscode)
                            {
                                responseCode = HttpStatusCode.PartialContent;
                            }

                            return canTranscode;
                        }).ToArray();

                        if (filteredStreams.Length != resultStreams.Length)
                        {
                            responseCode = HttpStatusCode.PartialContent;
                        }

                        resultStreams = filteredStreams;
                    }

                    if (resultStreams.Length == 0)
                    {
                        throw new DataStoreException(HttpStatusCode.NotAcceptable);
                    }

                    resultStreams = resultStreams.Select(x =>
                        new LazyTransformReadOnlyStream<Stream>(
                            x,
                            y => y.EncodeDicomFileAsDicom(parsedDicomTransferSyntax))).ToArray();
                }

                return new RetrieveDicomResourceResponse(responseCode, resultStreams);
            }
            catch (DataStoreException e)
            {
                return new RetrieveDicomResourceResponse(e.StatusCode);
            }
        }
    }
}
