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
using Microsoft.Health.Dicom.Core.Features.Resources.Store;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    public class RetrieveDicomResourceHandler : IRequestHandler<RetrieveDicomResourceRequest, RetrieveDicomResourceResponse>
    {
        private readonly IDicomBlobDataStore _dicomBlobDataStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;
        private static readonly DicomTransferSyntax DefaultTransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;

        public RetrieveDicomResourceHandler(
            IDicomBlobDataStore dicomBlobDataStore,
            IDicomMetadataStore dicomMetadataStore)
        {
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));

            _dicomBlobDataStore = dicomBlobDataStore;
            _dicomMetadataStore = dicomMetadataStore;
        }

        public async Task<RetrieveDicomResourceResponse> Handle(
            RetrieveDicomResourceRequest message, CancellationToken cancellationToken)
        {
            IEnumerable<DicomInstance> instancesToRetrieve;

            try
            {
                switch (message.DicomResource)
                {
                    case DicomFrames dicomInstance:
                        instancesToRetrieve = new[] { dicomInstance.Instance };
                        break;
                    case DicomInstance dicomInstance:
                        instancesToRetrieve = new[] { dicomInstance };
                        break;
                    case DicomSeries dicomSeries:
                        instancesToRetrieve = await _dicomMetadataStore.GetInstancesInSeriesAsync(dicomSeries.StudyInstanceUID, dicomSeries.SeriesInstanceUID, cancellationToken);
                        break;
                    case DicomStudy dicomStudy:
                        instancesToRetrieve = await _dicomMetadataStore.GetInstancesInStudyAsync(dicomStudy.StudyInstanceUID, cancellationToken);
                        break;
                    default:
                        throw new ArgumentException($"Unkown message type: {message.DicomResource.GetType()}", nameof(message));
                }

                Stream[] resultStreams = await Task.WhenAll(
                                                    instancesToRetrieve.Select(
                                                        x => _dicomBlobDataStore.GetFileAsStreamAsync(
                                                            StoreDicomResourcesHandler.GetBlobStorageName(x), cancellationToken)));

                DicomTransferSyntax parsedDicomTransferSyntax = string.IsNullOrWhiteSpace(message.RequestedTransferSyntax) ?
                                                    DefaultTransferSyntax :
                                                    DicomTransferSyntax.Parse(message.RequestedTransferSyntax);

                if (message.DicomResource is DicomFrames dicomFrames)
                {
                    // We first validate the file has the requested frames, then pass the frame for lazy encoding.
                    var dicomFile = DicomFile.Open(resultStreams.Single());
                    ValidateHasFrames(dicomFile, dicomFrames.Frames);

                    resultStreams = dicomFrames.Frames.Select(
                        x => new LazyTransformStream<DicomFile>(dicomFile, y => GetFrame(y, x, parsedDicomTransferSyntax)))
                        .ToArray();
                }
                else
                {
                    resultStreams = resultStreams.Select(x => new LazyTransformStream<Stream>(x, y => EncodeDicomFile(y, parsedDicomTransferSyntax))).ToArray();
                }

                return new RetrieveDicomResourceResponse(HttpStatusCode.OK, resultStreams);
            }
            catch (DataStoreException e)
            {
                return new RetrieveDicomResourceResponse(e.StatusCode);
            }
        }

        private static Stream EncodeDicomFile(Stream stream, DicomTransferSyntax requestedTransferSyntax)
        {
            var tempDicomFile = DicomFile.Open(stream);

            // If the DICOM file is already in the requested transfer syntax, return the base stream, otherwise re-encode.
            if (tempDicomFile.Dataset.InternalTransferSyntax == requestedTransferSyntax)
            {
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
            else
            {
                tempDicomFile = tempDicomFile.Clone(requestedTransferSyntax);

                var resultStream = new MemoryStream();
                tempDicomFile.Save(resultStream);
                resultStream.Seek(0, SeekOrigin.Begin);

                // We can dispose of the base stream as this is not needed.
                stream.Dispose();
                return resultStream;
            }
        }

        private static Stream GetFrame(DicomFile dicomFile, int frame, DicomTransferSyntax dicomTransferSyntax)
        {
            DicomDataset dataset = dicomFile.Dataset;
            IByteBuffer resultByteBuffer;

            if (dataset.InternalTransferSyntax.IsEncapsulated)
            {
                // Decompress single frame from source dataset
                var transcoder = new DicomTranscoder(
                    inputSyntax: dataset.InternalTransferSyntax,
                    outputSyntax: dicomTransferSyntax);

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

            return new MemoryStream(resultByteBuffer.Data);
        }

        private static void ValidateHasFrames(DicomFile dicomFile, IEnumerable<int> frames)
        {
            DicomDataset dataset = dicomFile.Dataset;

            // Validate the dataset has the correct DICOM tags (validating the pixel data last)
            if (!dataset.TryGetSingleValue(DicomTag.BitsAllocated, out ushort _) ||
                !dataset.TryGetSingleValue(DicomTag.Columns, out ushort _) ||
                !dataset.TryGetSingleValue(DicomTag.Rows, out ushort _) ||
                !dataset.TryGetValues(DicomTag.PixelData, out byte[] _))
            {
                throw new DataStoreException(HttpStatusCode.NotFound);
            }

            // Pull uncompressed frame from source pixel data
            var pixelData = DicomPixelData.Create(dataset);
            var missingFrames = frames.Where(x => x >= pixelData.NumberOfFrames).ToList();

            if (missingFrames.Count > 0)
            {
                throw new DataStoreException(HttpStatusCode.NotFound, new ArgumentException($"The frames '{string.Join(", ", missingFrames)}' do not exist.", nameof(frames)));
            }
        }
    }
}
