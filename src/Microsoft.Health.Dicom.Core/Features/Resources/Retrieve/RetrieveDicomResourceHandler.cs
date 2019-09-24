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
using Dicom.Imaging;
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

        private static readonly HashSet<DicomTransferSyntax> _supportedTransferSyntaxes8bit = new HashSet<DicomTransferSyntax>
        {
            DicomTransferSyntax.DeflatedExplicitVRLittleEndian,
            DicomTransferSyntax.ExplicitVRBigEndian,
            DicomTransferSyntax.ExplicitVRLittleEndian,
            DicomTransferSyntax.ImplicitVRLittleEndian,
            DicomTransferSyntax.JPEG2000Lossless,
            DicomTransferSyntax.JPEG2000Lossy,
            DicomTransferSyntax.JPEGProcess1,
            DicomTransferSyntax.JPEGProcess2_4,
            DicomTransferSyntax.RLELossless,
        };

        private static readonly HashSet<DicomTransferSyntax> _supportedTransferSyntaxesOver8bit = new HashSet<DicomTransferSyntax>
        {
            DicomTransferSyntax.DeflatedExplicitVRLittleEndian,
            DicomTransferSyntax.ExplicitVRBigEndian,
            DicomTransferSyntax.ExplicitVRLittleEndian,
            DicomTransferSyntax.ImplicitVRLittleEndian,
            DicomTransferSyntax.RLELossless,
        };

        public RetrieveDicomResourceHandler(IDicomMetadataStore dicomMetadataStore, DicomDataStore dicomDataStore)
            : base(dicomMetadataStore)
        {
            EnsureArg.IsNotNull(dicomDataStore, nameof(dicomDataStore));

            _dicomDataStore = dicomDataStore;
        }

        private bool CanTranscodeDataset(DicomDataset ds, DicomTransferSyntax toTransferSyntax)
        {
            if (toTransferSyntax == null)
            {
               return true;
            }

            var fromTs = ds.InternalTransferSyntax;
            if (!ds.TryGetSingleValue(DicomTag.BitsAllocated, out ushort bpp))
            {
                return false;
            }

            if (!ds.TryGetString(DicomTag.PhotometricInterpretation, out string photometricInterpretation))
            {
                return false;
            }

            // Bug in fo-dicom 4.0.1
            if ((toTransferSyntax == DicomTransferSyntax.JPEGProcess1 || toTransferSyntax == DicomTransferSyntax.JPEGProcess2_4) &&
                ((photometricInterpretation == PhotometricInterpretation.Monochrome2.Value) ||
                 (photometricInterpretation == PhotometricInterpretation.Monochrome1.Value)))
            {
                return false;
            }

            if (((bpp > 8) && _supportedTransferSyntaxesOver8bit.Contains(toTransferSyntax) && _supportedTransferSyntaxesOver8bit.Contains(fromTs)) ||
                 ((bpp <= 8) && _supportedTransferSyntaxes8bit.Contains(toTransferSyntax) && _supportedTransferSyntaxes8bit.Contains(fromTs)))
            {
                return true;
            }

            return false;
        }

        public async Task<RetrieveDicomResourceResponse> Handle(
            RetrieveDicomResourceRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            try
            {
                IEnumerable<DicomInstance> retrieveInstances = await GetInstancesToRetrieve(
                    message.ResourceType, message.StudyInstanceUID, message.SeriesInstanceUID, message.SopInstanceUID, cancellationToken);
                Stream[] resultStreams = await Task.WhenAll(retrieveInstances.Select(x => _dicomDataStore.GetDicomDataStreamAsync(x, cancellationToken)));

                var responseCode = HttpStatusCode.OK;

                DicomTransferSyntax parsedDicomTransferSyntax =
                    message.RenderedRequested ? null :
                    message.OriginalTransferSyntaxRequested() ? null :
                    string.IsNullOrWhiteSpace(message.RequestedRepresentation) ? DefaultTransferSyntax :
                    DicomTransferSyntax.Parse(message.RequestedRepresentation);

                ImageRepresentationModel imageRepresentation =
                    message.RenderedRequested ?
                        ImageRepresentationModel.Parse(message.RequestedRepresentation) :
                        null;

                if (message.ResourceType == ResourceType.Frames)
                {
                    // We first validate the file has the requested frames, then pass the frame for lazy encoding.
                    var dicomFile = DicomFile.Open(resultStreams.Single());
                    dicomFile.ValidateHasFrames(message.Frames);

                    if (message.RenderedRequested)
                    {
                        resultStreams = message.Frames.Select(
                                x => new LazyTransformReadOnlyStream<DicomFile>(
                                    dicomFile,
                                    y => y.GetFrameAsImage(x, imageRepresentation, message.ThumbnailRequested)))
                            .ToArray();
                    }
                    else
                    {
                        if (!message.OriginalTransferSyntaxRequested() &&
                            !CanTranscodeDataset(dicomFile.Dataset, parsedDicomTransferSyntax))
                        {
                            throw new DataStoreException(HttpStatusCode.NotAcceptable);
                        }

                        resultStreams = message.Frames.Select(
                                x => new LazyTransformReadOnlyStream<DicomFile>(
                                    dicomFile,
                                    y => y.GetFrameAsDicomData(x, parsedDicomTransferSyntax)))
                            .ToArray();
                    }
                }
                else
                {
                    if (message.RenderedRequested)
                    {
                        resultStreams = resultStreams.Select(x =>
                            new LazyTransformReadOnlyStream<Stream>(
                                x,
                                y => y.EncodeDicomFileAsImage(imageRepresentation, message.ThumbnailRequested))).ToArray();
                    }
                    else
                    {
                        if (!message.OriginalTransferSyntaxRequested())
                        {
                            resultStreams = resultStreams.Where(x =>
                            {
                                var canTranscode = false;

                                try
                                {
                                    // TODO: replace with FileReadOption.SkipLargeTags when updating to a future
                                    // version of fo-dicom where https://github.com/fo-dicom/fo-dicom/issues/893 is fixed
                                    var dicomFile = DicomFile.Open(x, FileReadOption.ReadLargeOnDemand);
                                    canTranscode = CanTranscodeDataset(dicomFile.Dataset, parsedDicomTransferSyntax);
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
