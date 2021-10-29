// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using Efferent.Native.Codec;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.IO;
using FellowOakDicom.Imaging.Codec;
using FellowOakDicom.IO.Buffer;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class Transcoder : ITranscoder
    {
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly ILogger<Transcoder> _logger;
        private static readonly Action<ILogger, int, string, string, Exception> LogTranscodingFrameErrorDelegate = LoggerMessage.Define<int, string, string>(
           logLevel: LogLevel.Error,
           eventId: default,
           formatString: "Failed to transcode frame {FrameIndex} from {InputTransferSyntax} to {OutputTransferSyntax}");

        private static readonly Action<ILogger, string, string, Exception> LogTranscodingFileErrorDelegate = LoggerMessage.Define<string, string>(
          logLevel: LogLevel.Error,
          eventId: default,
          formatString: "Failed to transcode dicom file from {InputTransferSyntax} to {OutputTransferSyntax}");

        public Transcoder(RecyclableMemoryStreamManager recyclableMemoryStreamManager, ILogger<Transcoder> logger)
        {
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
            EnsureArg.IsNotNull(logger, nameof(logger));
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
            _logger = logger;

            // Use Efferent transcoder
            TranscoderManager.SetImplementation(new NativeTranscoderManager());
        }

        public async Task<Stream> TranscodeFileAsync(Stream stream, string requestedTransferSyntax)
        {
            EnsureArg.IsNotNull(stream, nameof(stream));
            EnsureArg.IsNotEmptyOrWhiteSpace(requestedTransferSyntax, nameof(requestedTransferSyntax));
            var parsedDicomTransferSyntax = DicomTransferSyntax.Parse(requestedTransferSyntax);

            DicomFile dicomFile;

            try
            {
                dicomFile = await DicomFile.OpenAsync(stream, FileReadOption.ReadLargeOnDemand);
            }
            catch (DicomFileException)
            {
                throw;
            }

            stream.Seek(0, SeekOrigin.Begin);

            return await TranscodeFileAsync(dicomFile, parsedDicomTransferSyntax);
        }

        public Stream TranscodeFrame(DicomFile dicomFile, int frameIndex, string requestedTransferSyntax)
        {
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));
            EnsureArg.IsNotEmptyOrWhiteSpace(requestedTransferSyntax, nameof(requestedTransferSyntax));
            DicomDataset dataset = dicomFile.Dataset;

            // Validate requested frame index exists in file.
            dicomFile.GetPixelDataAndValidateFrames(new[] { frameIndex });
            var parsedDicomTransferSyntax = DicomTransferSyntax.Parse(requestedTransferSyntax);

            IByteBuffer resultByteBuffer = TranscodeFrame(dataset, frameIndex, parsedDicomTransferSyntax);
            return _recyclableMemoryStreamManager.GetStream("RetrieveDicomResourceHandler.GetFrameAsDicomData", resultByteBuffer.Data, 0, resultByteBuffer.Data.Length);
        }

        private IByteBuffer TranscodeFrame(DicomDataset dataset, int frameIndex, DicomTransferSyntax targetSyntax)
        {
            // DicomTranscoder doesn't support transcoding frame(s), one workaround is to transcode entire dicomdataset, and return required frame, which would be inefficent when there are multiple frames (imaging 100 frames in one dataset).
            // A better workaround is to create a dataset including only the required frame, and transcode it.
            try
            {
                DicomDataset datasetForFrame = CreateDatasetForFrame(dataset, frameIndex);
                var transcoder = new DicomTranscoder(dataset.InternalTransferSyntax, targetSyntax);
                DicomDataset result = transcoder.Transcode(datasetForFrame);
                return DicomPixelData.Create(result).GetFrame(0);
            }
            catch (Exception ex)
            {
                LogTranscodingFrameErrorDelegate(_logger, frameIndex, dataset?.InternalTransferSyntax?.UID?.UID, targetSyntax?.UID?.UID, ex);
                throw new TranscodingException();
            }
        }

        private static DicomDataset CreateDatasetForFrame(DicomDataset dataset, int frameIndex)
        {
            IByteBuffer frameData = DicomPixelData.Create(dataset).GetFrame(frameIndex);
            DicomDataset newDataset = dataset.Clone();
            var newdata = DicomPixelData.Create(newDataset, true);
            newdata.AddFrame(frameData);
            return newDataset;
        }

        private async Task<Stream> TranscodeFileAsync(DicomFile dicomFile, DicomTransferSyntax requestedTransferSyntax)
        {
            try
            {
                var transcoder = new DicomTranscoder(
                    dicomFile.Dataset.InternalTransferSyntax,
                    requestedTransferSyntax);
                dicomFile = transcoder.Transcode(dicomFile);
            }
            catch (Exception ex)
            {
                LogTranscodingFileErrorDelegate(_logger, dicomFile?.Dataset?.InternalTransferSyntax?.UID?.UID, requestedTransferSyntax?.UID?.UID, ex);

                // TODO: Reevaluate this while fixing transcoding handling.
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

                throw new TranscodingException();
            }

            MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream();

            if (dicomFile != null)
            {
                await dicomFile.SaveAsync(resultStream);
                resultStream.Seek(offset: 0, loc: SeekOrigin.Begin);
            }

            return resultStream;
        }
    }
}
