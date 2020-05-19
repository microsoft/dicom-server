// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class TranscoderTests
    {
        private ITranscoder _transcoder;
        private RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        private static readonly HashSet<DicomTransferSyntax> SupportedTransferSyntaxesFor8BitTranscoding = new HashSet<DicomTransferSyntax>
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

        private static readonly HashSet<DicomTransferSyntax> SupportedTransferSyntaxesForOver8BitTranscoding = new HashSet<DicomTransferSyntax>
        {
            DicomTransferSyntax.DeflatedExplicitVRLittleEndian,
            DicomTransferSyntax.ExplicitVRBigEndian,
            DicomTransferSyntax.ExplicitVRLittleEndian,
            DicomTransferSyntax.ImplicitVRLittleEndian,
            DicomTransferSyntax.RLELossless,
        };

        private static readonly HashSet<PhotometricInterpretation> SupportedPhotometricInterpretations = new HashSet<PhotometricInterpretation>
        {
            PhotometricInterpretation.Monochrome1,
            PhotometricInterpretation.Monochrome2,
            PhotometricInterpretation.PaletteColor,
            PhotometricInterpretation.Rgb,
            PhotometricInterpretation.YbrFull,
            PhotometricInterpretation.YbrFull422,
            PhotometricInterpretation.YbrPartial422,
            PhotometricInterpretation.YbrPartial420,
            PhotometricInterpretation.YbrIct,
            PhotometricInterpretation.YbrRct,
        };

        public TranscoderTests()
        {
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            _transcoder = new Transcoder(_recyclableMemoryStreamManager);
        }

        [Theory]
        [MemberData(nameof(Get16BitTranscoderCombos))]
        public async Task GivenSupported16bitTransferSyntax_WhenRetrievingFileAndAskingForConversion_ReturnedFileHasExpectedTransferSyntax(
            DicomTransferSyntax tsFrom,
            DicomTransferSyntax tsTo,
            PhotometricInterpretation photometricInterpretation)
        {
            (DicomFile dicomFile, Stream stream) = await StreamAndStoredFileFromDataset(photometricInterpretation, false, tsFrom);
            dicomFile.Dataset.ToInstanceIdentifier();

            Stream transcodedFile = await _transcoder.TranscodeFile(stream, tsTo.UID.UID);

            ValidateTransferSyntax(tsTo, transcodedFile);
        }

        [Theory]
        [MemberData(nameof(Get16BitTranscoderCombos))]
        public void GivenSupported16bitTransferSyntax_WhenRetrievingFrameAndAskingForConversion_ReturnedFileHasExpectedTransferSyntax(
            DicomTransferSyntax tsFrom,
            DicomTransferSyntax tsTo,
            PhotometricInterpretation photometricInterpretation)
        {
            DicomFile dicomFile = StreamAndStoredFileFromDataset(photometricInterpretation, false, tsFrom).Result.dicomFile;
            dicomFile.Dataset.ToInstanceIdentifier();

            _transcoder.TranscodeFrame(dicomFile, 1, tsTo.UID.UID);
        }

        [Theory]
        [MemberData(nameof(GetSupported8BitTranscoderCombos))]
        public async Task GivenSupported8bitTransferSyntax_WhenRetrievingFileAndAskingForConversion_FileIsReturnedWhenExpected(
            DicomTransferSyntax tsFrom,
            DicomTransferSyntax tsTo,
            PhotometricInterpretation photometricInterpretation)
        {
            (DicomFile dicomFile, Stream stream) = await StreamAndStoredFileFromDataset(photometricInterpretation, true, tsFrom);
            dicomFile.Dataset.ToInstanceIdentifier();
            Stream transcodedFile = await _transcoder.TranscodeFile(stream, tsTo.UID.UID);

            ValidateTransferSyntax(tsTo, transcodedFile);
        }

        [Theory]
        [MemberData(nameof(GetSupported8BitTranscoderCombos))]
        public void GivenSupported8bitTransferSyntax_WhenRetrievingFrameAndAskingForConversion_FileIsReturnedWhenExpected(
            DicomTransferSyntax tsFrom,
            DicomTransferSyntax tsTo,
            PhotometricInterpretation photometricInterpretation)
        {
            DicomFile dicomFile = StreamAndStoredFileFromDataset(photometricInterpretation, true, tsFrom).Result.dicomFile;
            dicomFile.Dataset.ToInstanceIdentifier();
            _transcoder.TranscodeFrame(dicomFile, 1, tsTo.UID.UID);
        }

        [Theory]
        [MemberData(nameof(GetUnsupported8BitJPEGTranscoderCombos))]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "Consistency with other tests.")]
        public void GivenUnsupported8bitFromJPEGFTransferSyntax_WhenRetrievingFileAndAskingForConversion_ErrorIsThrown(
            DicomTransferSyntax tsFrom,
            DicomTransferSyntax tsTo,
            PhotometricInterpretation photometricInterpretation)
        {
            Assert.Throws<AggregateException>(() => StreamAndStoredFileFromDataset(photometricInterpretation, true, tsFrom).Result);
        }

        [Theory]
        [MemberData(nameof(GetUnsupported8BitJPEGTranscoderCombos))]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "Consistency with other tests.")]
        public void GivenUnsupported8bitFromJPEGFTransferSyntax_WhenRetrievingFrameAndAskingForConversion_ErrorIsThrown(
            DicomTransferSyntax tsFrom,
            DicomTransferSyntax tsTo,
            PhotometricInterpretation photometricInterpretation)
        {
            Assert.Throws<AggregateException>(() => StreamAndStoredFileFromDataset(photometricInterpretation, true, tsFrom).Result);
        }

        [Theory]
        [MemberData(nameof(GetUnsupported8BitMonochromeTranscoderCombos))]
        public async Task GivenUnsupported8bitMonochromeTransferSyntax_WhenRetrievingFileAndAskingForConversion_ErrorIsThrown(
            DicomTransferSyntax tsFrom,
            DicomTransferSyntax tsTo,
            PhotometricInterpretation photometricInterpretation)
        {
            (DicomFile dicomFile, Stream stream) = await StreamAndStoredFileFromDataset(photometricInterpretation, true, tsFrom);
            var dicomInstance = dicomFile.Dataset.ToInstanceIdentifier();
            var ex = Assert.ThrowsAsync<TranscodingException>(() => _transcoder.TranscodeFile(stream, tsTo.UID.UID));

            Assert.Equal(DicomCoreResource.UnsupportedTranscoding, ex.Result.Message);
        }

        [Theory]
        [MemberData(nameof(GetUnsupported8BitMonochromeTranscoderCombos))]
        public void GivenUnsupported8bitMonochromeTransferSyntax_WhenRetrievingFrameAndAskingForConversion_ErrorIsThrown(
            DicomTransferSyntax tsFrom,
            DicomTransferSyntax tsTo,
            PhotometricInterpretation photometricInterpretation)
        {
            DicomFile dicomFile = StreamAndStoredFileFromDataset(photometricInterpretation, true, tsFrom).Result.dicomFile;
            var dicomInstance = dicomFile.Dataset.ToInstanceIdentifier();
            var ex = Assert.Throws<TranscodingException>(() => _transcoder.TranscodeFrame(dicomFile, 1, tsTo.UID.UID));

            Assert.Equal(DicomCoreResource.UnsupportedTranscoding, ex.Message);
        }

        public static IEnumerable<object[]> GetSupported8BitTranscoderCombos()
        {
            HashSet<DicomTransferSyntax> fromList = SupportedTransferSyntaxesFor8BitTranscoding;
            HashSet<DicomTransferSyntax> toList = SupportedTransferSyntaxesFor8BitTranscoding;
            HashSet<PhotometricInterpretation> photometricInterpretations = SupportedPhotometricInterpretations;

            HashSet<(DicomTransferSyntax fromTs, DicomTransferSyntax toTs, PhotometricInterpretation photometricInterpretation)> supported8BitTranscoderCombos =
                (from x in fromList from y in toList from z in photometricInterpretations select (x, y, z)).ToHashSet();

            supported8BitTranscoderCombos.ExceptWith(GenerateUnsupported8BitJPEG2000TranscoderCombos());
            supported8BitTranscoderCombos.ExceptWith(GenerateUnsupported8BitJPEGTranscoderCombos());
            supported8BitTranscoderCombos.ExceptWith(GenerateUnsupported8BitMonochromeTranscoderCombos());
            supported8BitTranscoderCombos.ExceptWith(GenerateUnsupported8BitJPEGProcessMonochromeTranscoderCombos());

            return from x in supported8BitTranscoderCombos select new object[] { x.fromTs, x.toTs, x.photometricInterpretation };
        }

        public static IEnumerable<object[]> GetUnsupported8BitJPEGTranscoderCombos()
        {
            HashSet<(DicomTransferSyntax fromTs, DicomTransferSyntax toTs, PhotometricInterpretation photometricInterpretation)> supported8BitTranscoderCombos =
                new HashSet<(DicomTransferSyntax, DicomTransferSyntax, PhotometricInterpretation)>();

            supported8BitTranscoderCombos.UnionWith(GenerateUnsupported8BitJPEG2000TranscoderCombos());
            supported8BitTranscoderCombos.UnionWith(GenerateUnsupported8BitJPEGTranscoderCombos());

            return from x in supported8BitTranscoderCombos select new object[] { x.fromTs, x.toTs, x.photometricInterpretation };
        }

        public static IEnumerable<object[]> GetUnsupported8BitMonochromeTranscoderCombos()
        {
            HashSet<(DicomTransferSyntax fromTs, DicomTransferSyntax toTs, PhotometricInterpretation photometricInterpretation)> supported8BitTranscoderCombos =
                new HashSet<(DicomTransferSyntax, DicomTransferSyntax, PhotometricInterpretation)>();

            supported8BitTranscoderCombos.UnionWith(GenerateUnsupported8BitMonochromeTranscoderCombos());
            supported8BitTranscoderCombos.UnionWith(GenerateUnsupported8BitJPEGProcessMonochromeTranscoderCombos());

            return from x in supported8BitTranscoderCombos select new object[] { x.fromTs, x.toTs, x.photometricInterpretation };
        }

        public static IEnumerable<object[]> Get16BitTranscoderCombos()
        {
            HashSet<DicomTransferSyntax> fromList = SupportedTransferSyntaxesForOver8BitTranscoding;
            HashSet<DicomTransferSyntax> toList = SupportedTransferSyntaxesForOver8BitTranscoding;
            HashSet<PhotometricInterpretation> photometricInterpretations = SupportedPhotometricInterpretations;

            return from x in fromList from y in toList from z in photometricInterpretations select new object[] { x, y, z };
        }

        public static HashSet<(DicomTransferSyntax, DicomTransferSyntax, PhotometricInterpretation)> GenerateUnsupported8BitJPEG2000TranscoderCombos()
        {
            HashSet<DicomTransferSyntax> fromTs = new HashSet<DicomTransferSyntax>
            {
                DicomTransferSyntax.JPEG2000Lossless,
                DicomTransferSyntax.JPEG2000Lossy,
            };
            HashSet<DicomTransferSyntax> toTs = new HashSet<DicomTransferSyntax>
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
            HashSet<PhotometricInterpretation> photometricInterpretations = new HashSet<PhotometricInterpretation>
            {
                PhotometricInterpretation.Rgb,
                PhotometricInterpretation.YbrFull422,
                PhotometricInterpretation.YbrPartial422,
                PhotometricInterpretation.YbrPartial420,
            };

            return (from x in fromTs from y in toTs from z in photometricInterpretations select (x, y, z)).ToHashSet();
        }

        public static HashSet<(DicomTransferSyntax, DicomTransferSyntax, PhotometricInterpretation)> GenerateUnsupported8BitJPEGTranscoderCombos()
        {
            HashSet<DicomTransferSyntax> fromTs = new HashSet<DicomTransferSyntax>
            {
                DicomTransferSyntax.JPEGProcess1,
                DicomTransferSyntax.JPEGProcess2_4,
            };
            HashSet<DicomTransferSyntax> toTs = new HashSet<DicomTransferSyntax>
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
            HashSet<PhotometricInterpretation> photometricInterpretations = new HashSet<PhotometricInterpretation>
            {
                PhotometricInterpretation.Rgb,
                PhotometricInterpretation.YbrFull,
                PhotometricInterpretation.YbrFull422,
                PhotometricInterpretation.YbrPartial422,
                PhotometricInterpretation.YbrPartial420,
                PhotometricInterpretation.YbrIct,
                PhotometricInterpretation.YbrRct,
            };

            return (from x in fromTs from y in toTs from z in photometricInterpretations select (x, y, z)).ToHashSet();
        }

        public static HashSet<(DicomTransferSyntax, DicomTransferSyntax, PhotometricInterpretation)> GenerateUnsupported8BitMonochromeTranscoderCombos()
        {
            HashSet<DicomTransferSyntax> fromTs = new HashSet<DicomTransferSyntax>
            {
                DicomTransferSyntax.DeflatedExplicitVRLittleEndian,
                DicomTransferSyntax.ExplicitVRBigEndian,
                DicomTransferSyntax.ExplicitVRLittleEndian,
                DicomTransferSyntax.ImplicitVRLittleEndian,
                DicomTransferSyntax.JPEG2000Lossless,
                DicomTransferSyntax.JPEG2000Lossy,
                DicomTransferSyntax.RLELossless,
            };
            HashSet<DicomTransferSyntax> toTs = new HashSet<DicomTransferSyntax>
            {
                DicomTransferSyntax.JPEGProcess1,
                DicomTransferSyntax.JPEGProcess2_4,
            };
            HashSet<PhotometricInterpretation> photometricInterpretations = new HashSet<PhotometricInterpretation>
            {
                PhotometricInterpretation.Monochrome1,
                PhotometricInterpretation.Monochrome2,
            };

            return (from x in fromTs from y in toTs from z in photometricInterpretations select (x, y, z)).ToHashSet();
        }

        public static HashSet<(DicomTransferSyntax, DicomTransferSyntax, PhotometricInterpretation)> GenerateUnsupported8BitJPEGProcessMonochromeTranscoderCombos()
        {
            // bug in fo-dicom doesn't set up photometric interpretation correctly.
            HashSet<DicomTransferSyntax> fromTs = new HashSet<DicomTransferSyntax>
            {
                DicomTransferSyntax.JPEGProcess1,
                DicomTransferSyntax.JPEGProcess2_4,
            };
            HashSet<DicomTransferSyntax> toTs = new HashSet<DicomTransferSyntax>
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
            HashSet<PhotometricInterpretation> photometricInterpretations = new HashSet<PhotometricInterpretation>
            {
                PhotometricInterpretation.Monochrome1,
                PhotometricInterpretation.Monochrome2,
                PhotometricInterpretation.PaletteColor,
            };

            return (from x in fromTs from y in toTs from z in photometricInterpretations select (x, y, z)).ToHashSet();
        }

        private async Task<(DicomFile dicomFile, Stream stream)> StreamAndStoredFileFromDataset(PhotometricInterpretation photometricInterpretation, bool is8BitPixelData, DicomTransferSyntax transferSyntax)
        {
            var dicomFile = is8BitPixelData ?
                Samples.CreateRandomDicomFileWith8BitPixelData(transferSyntax: transferSyntax.UID.UID, photometricInterpretation: photometricInterpretation.Value, frames: 2)
                : Samples.CreateRandomDicomFileWith16BitPixelData(transferSyntax: transferSyntax.UID.UID, photometricInterpretation: photometricInterpretation.Value, frames: 2);

            MemoryStream stream = _recyclableMemoryStreamManager.GetStream();
            await dicomFile.SaveAsync(stream);
            stream.Position = 0;

            return (dicomFile, stream);
        }

        private void ValidateTransferSyntax(
           DicomTransferSyntax expectedTransferSyntax,
           Stream responseStream)
        {
            DicomFile responseFile = DicomFile.Open(responseStream);

            Assert.Equal(expectedTransferSyntax, responseFile.Dataset.InternalTransferSyntax);
        }
    }
}
