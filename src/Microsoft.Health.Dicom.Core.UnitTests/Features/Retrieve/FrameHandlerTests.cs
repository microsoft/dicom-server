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
using EnsureThat.Enforcers;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class FrameHandlerTests
    {
        private IFrameHandler _frameHandler;
        private RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public FrameHandlerTests()
        {
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            _frameHandler = new FrameHandler(Substitute.For<ITranscoder>(), _recyclableMemoryStreamManager);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1, 2)]
        [InlineData(0, 1, 2)]
        public async Task GivenDicomFileWithFrames_WhenRetrievingFrameWithOriginalTransferSyntax_ThenExpectedFramesAreReturned(params int[] frames)
        {
            (DicomFile file, Stream stream) = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(), 3).Result;
            IReadOnlyCollection<Stream> framesStream = await _frameHandler.GetFramesResourceAsync(stream, frames, true, "*");
            var framesOutput = framesStream.ToArray();

            for (int i = 0; i < frames.Length; i++)
            {
                AssertPixelDataEqual(DicomPixelData.Create(file.Dataset).GetFrame(frames[i]), framesOutput[i]);
            }
        }

        [Theory]
        [InlineData(0)]
        public async Task GivenDicomFileWithoutFrames_WhenRetrievingFrameWithOriginalTransferSyntax_ThenFrameNotFoundExceptionIsThrown(params int[] frames)
        {
            (DicomFile file, Stream stream) = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers()).Result;
            await Assert.ThrowsAsync<FrameNotFoundException>(() => _frameHandler.GetFramesResourceAsync(stream, frames, true, "*"));
        }

        [Theory]
        [InlineData(3)]
        [InlineData(0, 3)]
        [InlineData(0, 1, 2, 3)]
        public async Task GivenDicomFileWithFrames_WhenRetrievingNonExistentFrameWithOriginalTransferSyntax_ThenFrameNotFoundExceptionIsThrown(params int[] frames)
        {
            (DicomFile file, Stream stream) = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(), 3).Result;
            await Assert.ThrowsAsync<FrameNotFoundException>(() => _frameHandler.GetFramesResourceAsync(stream, frames, true, "*"));
        }

        [Theory]
        [MemberData(nameof(TestDataForInvokingTranscoderTests))]
        public async Task GivenDicomFileWithFrames_WhenRetrievingWithTransferSyntax_ThenTranscoderShouldBeInvokedAsExpected(bool originalTransferSyntaxRequested, string requestedRepresentation, bool shouldBeInvoked)
        {
            (DicomFile file, Stream stream) = StreamAndStoredFileFromDataset(GenerateDatasetsFromIdentifiers(), 1).Result;
            ITranscoder transcoder = Substitute.For<ITranscoder>();
            transcoder.TranscodeFrame(Arg.Any<DicomFile>(), Arg.Any<int>(), Arg.Any<string>()).Returns(_recyclableMemoryStreamManager.GetStream());
            FrameHandler frameHandler = new FrameHandler(transcoder, _recyclableMemoryStreamManager);
            IReadOnlyCollection<Stream> result = await frameHandler.GetFramesResourceAsync(stream, new int[] { 0 }, originalTransferSyntaxRequested, requestedRepresentation);

            // Call Position of LazyTransformReadOnlyStream so that transcoder.TranscodeFrame is invoked
            long pos = result.First().Position;
            if (shouldBeInvoked)
            {
                transcoder.Received().TranscodeFrame(Arg.Any<DicomFile>(), Arg.Any<int>(), Arg.Any<string>());
            }
            else
            {
                transcoder.DidNotReceive().TranscodeFrame(Arg.Any<DicomFile>(), Arg.Any<int>(), Arg.Any<string>());
            }
        }

        public static IEnumerable<object[]> TestDataForInvokingTranscoderTests()
        {
            yield return new object[] { true, DicomTransferSyntaxUids.Original, false };
            yield return new object[] { false, DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID, false };
            yield return new object[] { true, DicomTransferSyntax.JPEGProcess1.UID.UID, false };
        }

        private DicomDataset GenerateDatasetsFromIdentifiers()
        {
            var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
            {
                { DicomTag.StudyInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.SeriesInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.SOPClassUID, TestUidGenerator.Generate() },
                { DicomTag.PatientID, TestUidGenerator.Generate() },
                { DicomTag.BitsAllocated, (ushort)8 },
                { DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value },
            };

            return ds;
        }

        private async Task<(DicomFile, Stream)> StreamAndStoredFileFromDataset(DicomDataset dataset, int frames = 0)
        {
            // Create DicomFile associated with input dataset with random pixel data.
            var dicomFile = new DicomFile(dataset);
            Samples.AppendRandomPixelData(5, 5, frames, dicomFile);

            MemoryStream stream = _recyclableMemoryStreamManager.GetStream();
            await dicomFile.SaveAsync(stream);
            stream.Position = 0;

            return (dicomFile, stream);
        }

        private static void AssertPixelDataEqual(IByteBuffer expectedPixelData, Stream actualPixelData)
        {
            Assert.Equal(expectedPixelData.Size, actualPixelData.Length);
            Assert.Equal(0, actualPixelData.Position);
            for (var i = 0; i < expectedPixelData.Size; i++)
            {
                Assert.Equal(expectedPixelData.Data[i], actualPixelData.ReadByte());
            }
        }
    }
}
