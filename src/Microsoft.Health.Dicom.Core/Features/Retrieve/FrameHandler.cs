// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;

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

    public async Task<IReadOnlyCollection<Stream>> GetFramesResourceAsync(Stream stream, IEnumerable<int> frames, bool originalTransferSyntaxSelected, string requestedTransferSyntax)
    {
        EnsureArg.IsNotNull(stream, nameof(stream));
        EnsureArg.IsNotNull(frames, nameof(frames));

        DicomFile dicomFile = await DicomFile.OpenAsync(stream);

        // Validate requested frame index exists in file and retrieve the pixel data associated with the file.
        DicomPixelData pixelData = dicomFile.GetPixelDataAndValidateFrames(frames);

        if (!originalTransferSyntaxSelected && !dicomFile.Dataset.InternalTransferSyntax.Equals(DicomTransferSyntax.Parse(requestedTransferSyntax)))
        {
            return frames.Select(frame => new LazyTransformReadOnlyStream<DicomFile>(
                    dicomFile,
                    df => _transcoder.TranscodeFrame(df, frame, requestedTransferSyntax)))
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

        IByteBuffer resultByteBuffer = pixelData.GetFrame(frame);

        return _recyclableMemoryStreamManager.GetStream("FrameHandler.GetFrameAsDicomData", resultByteBuffer.Data, 0, resultByteBuffer.Data.Length);
    }
}
