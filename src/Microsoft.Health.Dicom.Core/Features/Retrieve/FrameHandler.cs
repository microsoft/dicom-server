// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class FrameHandler : IFrameHandler
    {
        private readonly IRetrieveTranscoder _dicomRetrieveTranscoder;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public FrameHandler(
            IRetrieveTranscoder dicomRetrieveTranscoder,
            RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            EnsureArg.IsNotNull(dicomRetrieveTranscoder, nameof(dicomRetrieveTranscoder));
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

            _dicomRetrieveTranscoder = dicomRetrieveTranscoder;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        }

        public async Task<Stream[]> GetFramesResourceAsync(Stream stream, IEnumerable<int> frames, bool originalTransferSyntaxRequested, string requestedRepresentation)
        {
            var dicomFile = await DicomFile.OpenAsync(stream);
            dicomFile.ValidateHasFrames(frames);

            if (!originalTransferSyntaxRequested && dicomFile.Dataset.InternalTransferSyntax.IsEncapsulated)
            {
                return frames.Select(frame => new LazyTransformReadOnlyStream<DicomFile>(
                        dicomFile,
                        df => _dicomRetrieveTranscoder.TranscodeFrame(df, frame, requestedRepresentation)))
                .ToArray();
            }
            else
            {
                return frames.Select(
                        frame => new LazyTransformReadOnlyStream<DicomFile>(
                            dicomFile,
                            df => GetFrameAsDicomData(df, frame)))
                    .ToArray();
            }
        }

        private Stream GetFrameAsDicomData(DicomFile dicomFile, int frame)
        {
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));
            DicomDataset dataset = dicomFile.Dataset;

            IByteBuffer resultByteBuffer;

            // Pull uncompressed frame from source pixel data
            var pixelData = DicomPixelData.Create(dataset);
            if (frame >= pixelData.NumberOfFrames)
            {
                throw new FrameNotFoundException();
            }

            resultByteBuffer = pixelData.GetFrame(frame);

            return _recyclableMemoryStreamManager.GetStream("FrameHandler.GetFrameAsDicomData", resultByteBuffer.Data, 0, resultByteBuffer.Data.Length);
        }
    }
}
