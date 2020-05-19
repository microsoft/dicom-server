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
        private readonly ITranscoder _transcoder;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public FrameHandler(
            ITranscoder transcoder,
            RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            EnsureArg.IsNotNull(transcoder, nameof(transcoder));
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

            _transcoder = transcoder;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        }

        public async Task<IReadOnlyCollection<Stream>> GetFramesResourceAsync(Stream stream, IEnumerable<int> frames, bool originalTransferSyntaxRequested, string requestedRepresentation)
        {
            var dicomFile = await DicomFile.OpenAsync(stream);
            var pixelData = dicomFile.GetFrames(frames);

            if (!originalTransferSyntaxRequested && dicomFile.Dataset.InternalTransferSyntax.IsEncapsulated)
            {
                return frames.Select(frame => new LazyTransformReadOnlyStream<DicomFile>(
                        dicomFile,
                        df => _transcoder.TranscodeFrame(df, frame, requestedRepresentation)))
                .ToArray();
            }
            else
            {
                return frames.Select(
                        frame => new LazyTransformReadOnlyStream<DicomFile>(
                            dicomFile,
                            df => GetFrameAsDicomData(pixelData, frame)))
                    .ToArray();
            }
        }

        private Stream GetFrameAsDicomData(DicomPixelData pixelData, int frame)
        {
            EnsureArg.IsNotNull(pixelData, nameof(pixelData));
            if (frame >= pixelData.NumberOfFrames)
            {
                throw new FrameNotFoundException();
            }

            IByteBuffer resultByteBuffer = pixelData.GetFrame(frame);

            return _recyclableMemoryStreamManager.GetStream("FrameHandler.GetFrameAsDicomData", resultByteBuffer.Data, 0, resultByteBuffer.Data.Length);
        }
    }
}
