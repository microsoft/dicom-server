// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    public class RetrieveDicomResourceHandler : BaseRetrieveDicomResourceHandler, IRequestHandler<RetrieveDicomResourceRequest, RetrieveDicomResourceResponse>
    {
        private static readonly DicomTransferSyntax DefaultTransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
        private readonly IDicomDataStore _dicomDataStore;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public RetrieveDicomResourceHandler(IDicomMetadataStore dicomMetadataStore, IDicomDataStore dicomDataStore, RecyclableMemoryStreamManager recyclableMemoryStreamManager)
            : base(dicomMetadataStore)
        {
            EnsureArg.IsNotNull(dicomDataStore, nameof(dicomDataStore));
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

            _dicomDataStore = dicomDataStore;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
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
                            frame => new LazyTransformReadOnlyStream<DicomFile>(
                                dicomFile,
                                df => GetFrameAsDicomData(df, frame, parsedDicomTransferSyntax)))
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

                    resultStreams = resultStreams.Select(stream =>
                        new LazyTransformReadOnlyStream<Stream>(
                            stream,
                            s => EncodeDicomFileAsDicom(s, parsedDicomTransferSyntax))).ToArray();
                }

                return new RetrieveDicomResourceResponse(responseCode, resultStreams);
            }
            catch (DataStoreException e)
            {
                return new RetrieveDicomResourceResponse(e.StatusCode);
            }
        }

        private Stream GetFrameAsDicomData(DicomFile dicomFile, int frame, DicomTransferSyntax requestedTransferSyntax)
        {
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));
            DicomDataset dataset = dicomFile.Dataset;

            IByteBuffer resultByteBuffer;

            if (dataset.InternalTransferSyntax.IsEncapsulated && (requestedTransferSyntax != null))
            {
                // Decompress single frame from source dataset
                var transcoder = new DicomTranscoder(dataset.InternalTransferSyntax, requestedTransferSyntax);
                resultByteBuffer = transcoder.DecodeFrame(dataset, frame);
            }
            else
            {
                // Pull uncompressed frame from source pixel data
                var pixelData = DicomPixelData.Create(dataset);
                if (frame >= pixelData.NumberOfFrames)
                {
                    throw new DataStoreException(HttpStatusCode.NotFound, new ArgumentException($"The frame '{frame}' does not exist.", nameof(frame)));
                }

                resultByteBuffer = pixelData.GetFrame(frame);
            }

            return _recyclableMemoryStreamManager.GetStream("RetrieveDicomResourceHandler.GetFrameAsDicomData", resultByteBuffer.Data, 0, resultByteBuffer.Data.Length);
        }

        private Stream EncodeDicomFileAsDicom(Stream stream, DicomTransferSyntax requestedTransferSyntax)
        {
            var tempDicomFile = DicomFile.Open(stream);

            // If the DICOM file is already in the requested transfer syntax OR original transfer syntax is requested,
            // return the base stream, otherwise re-encode.
            if ((tempDicomFile.Dataset.InternalTransferSyntax == requestedTransferSyntax) ||
                (requestedTransferSyntax == null))
            {
                stream.Seek(offset: 0, SeekOrigin.Begin);
                return stream;
            }
            else
            {
                try
                {
                    var transcoder = new DicomTranscoder(
                        tempDicomFile.Dataset.InternalTransferSyntax,
                        requestedTransferSyntax);
                    tempDicomFile = transcoder.Transcode(tempDicomFile);
                }
                catch
                {
                    // We catch all here as Transcoder can throw a wide variety of things.
                    // Basically this means codec failure - a quite extraordinary situation, but not impossible
                    // Proper solution here would be to actually try transcoding all the files that we are
                    // returning and either form a PartialContent or NotAcceptable response with an extra error message in
                    // the headers. Because transcoding is an expensive operation, we choose to do it from within the
                    // LazyTransformReadOnlyStream at the time when response is being formed by the server, therefore this code
                    // is called from ASP.NET framework and at this point we can not change our server response.
                    // The decision for now is just to return an empty stream here letting the client handle it.
                    // In the future a more optimal solution may involve maintaining a cache of transcoded images and
                    // using that to determine if transcoding is possible from within the Handle method.

                    tempDicomFile = null;
                }

                MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream();

                if (tempDicomFile != null)
                {
                    tempDicomFile.Save(resultStream);
                    resultStream.Seek(offset: 0, loc: SeekOrigin.Begin);
                }

                // We can dispose of the base stream as this is not needed.
                stream.Dispose();
                return resultStream;
            }
        }
    }
}
