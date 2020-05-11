// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Linq;
using Dicom;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class RetrieveTranscoder : IRetrieveTranscoder
    {
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private static readonly DicomTransferSyntax DefaultTransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;

        public RetrieveTranscoder(
            RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        }

        public (bool, Stream[]) TranscodeFiles(Stream[] streams, string requestedTransferSyntax)
        {
            bool isPartialSuccess = false;
            DicomTransferSyntax parsedDicomTransferSyntax =
                   string.IsNullOrWhiteSpace(requestedTransferSyntax) ?
                       DefaultTransferSyntax :
                       DicomTransferSyntax.Parse(requestedTransferSyntax);

            Stream[] filteredStreams = streams.Where(x =>
            {
                var canTranscode = false;

                try
                {
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
                    isPartialSuccess = true;
                }

                return canTranscode;
            }).ToArray();

            if (filteredStreams.Length == 0)
            {
                throw new TranscodingException();
            }

            if (filteredStreams.Length != streams.Length)
            {
                isPartialSuccess = true;
            }

            filteredStreams = filteredStreams.Select(stream =>
                        new LazyTransformReadOnlyStream<Stream>(
                            stream,
                            s => TranscodeFile(s, parsedDicomTransferSyntax))).ToArray();

            return (isPartialSuccess, filteredStreams);
        }

        public Stream TranscodeFrame(DicomFile dicomFile, int frame, string requestedTransferSyntax)
        {
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));
            DicomDataset dataset = dicomFile.Dataset;

            DicomTransferSyntax parsedDicomTransferSyntax =
                   string.IsNullOrWhiteSpace(requestedTransferSyntax) ?
                       DefaultTransferSyntax :
                       DicomTransferSyntax.Parse(requestedTransferSyntax);

            IByteBuffer resultByteBuffer;

            if (!dicomFile.Dataset.CanTranscodeDataset(parsedDicomTransferSyntax))
            {
                throw new TranscodingException();
            }

            // Decompress single frame from source dataset
            var transcoder = new DicomTranscoder(dataset.InternalTransferSyntax, parsedDicomTransferSyntax);
            resultByteBuffer = transcoder.DecodeFrame(dataset, frame);

            return _recyclableMemoryStreamManager.GetStream("RetrieveDicomResourceHandler.GetFrameAsDicomData", resultByteBuffer.Data, 0, resultByteBuffer.Data.Length);
        }

        private Stream TranscodeFile(Stream stream, DicomTransferSyntax requestedTransferSyntax)
        {
            var tempDicomFile = DicomFile.Open(stream);

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
