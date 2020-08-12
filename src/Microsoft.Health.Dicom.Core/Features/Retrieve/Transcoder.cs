// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class Transcoder : ITranscoder
    {
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public Transcoder(
            RecyclableMemoryStreamManager recyclableMemoryStreamManager)
            : this(recyclableMemoryStreamManager, null)
        {
        }

        public Transcoder(RecyclableMemoryStreamManager recyclableMemoryStreamManager, TranscoderManager transcoderManager)
        {
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
            if (transcoderManager != null)
            {
                TranscoderManager.SetImplementation(transcoderManager);
            }
        }

        public async Task<Stream> TranscodeFileAsync(Stream stream, string requestedTransferSyntax)
        {
            DicomTransferSyntax parsedDicomTransferSyntax = DicomTransferSyntax.Parse(requestedTransferSyntax);

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
            DicomDataset dataset = dicomFile.Dataset;

            // Validate requested frame index exists in file.
            dicomFile.GetPixelDataAndValidateFrames(new[] { frameIndex });
            DicomTransferSyntax parsedDicomTransferSyntax = DicomTransferSyntax.Parse(requestedTransferSyntax);
            IByteBuffer resultByteBuffer = TranscodeFrame(dataset, frameIndex, parsedDicomTransferSyntax);
            return _recyclableMemoryStreamManager.GetStream("RetrieveDicomResourceHandler.GetFrameAsDicomData", resultByteBuffer.Data, 0, resultByteBuffer.Data.Length);
        }

        private IByteBuffer TranscodeFrame(DicomDataset dataset, int frameIndex, DicomTransferSyntax targetSyntax)
        {
            try
            {
                DicomDataset datasetForFrame = CreateDatasetForFrame(dataset, frameIndex);
                DicomTranscoder transcoder = new DicomTranscoder(dataset.InternalTransferSyntax, targetSyntax);
                DicomDataset result = transcoder.Transcode(datasetForFrame);
                return DicomPixelData.Create(result).GetFrame(0);
            }
            catch
            {
                throw new TranscodingException();
            }
        }

        private static DicomDataset CreateDatasetForFrame(DicomDataset dataset, int frameIndex)
        {
            IByteBuffer frameData = DicomPixelData.Create(dataset).GetFrame(frameIndex);
            DicomDataset newDataset = dataset.Clone();
            DicomPixelData newdata = DicomPixelData.Create(newDataset, true);
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
            catch
            {
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
