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
using EnsureThat;
using Microsoft.Extensions.Logging.Abstractions;
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
            _transcoder = new Transcoder(_recyclableMemoryStreamManager, NullLogger<Transcoder>.Instance);
        }

        [Theory]
        [MemberData(nameof(Get16BitTranscoderCombos))]
        public async Task GivenSupported16bitTransferSyntax_WhenRetrievingFileAndAskingForConversion_ReturnedFileHasExpectedTransferSyntax(
            DicomTransferSyntax tsFrom,
            DicomTransferSyntax tsTo,
            PhotometricInterpretation photometricInterpretation)
        {
            EnsureArg.IsNotNull(photometricInterpretation, nameof(photometricInterpretation));
            EnsureArg.IsNotNull(tsFrom, nameof(tsFrom));
            EnsureArg.IsNotNull(tsTo, nameof(tsTo));
            (DicomFile dicomFile, Stream stream) = await StreamAndStoredFileFromDataset(photometricInterpretation, false, tsFrom);
            dicomFile.Dataset.ToInstanceIdentifier();

            Stream transcodedFile = await _transcoder.TranscodeFileAsync(stream, tsTo.UID.UID);

            ValidateTransferSyntax(tsTo, transcodedFile);
        }

        [Theory]
        [MemberData(nameof(Get16BitTranscoderCombos))]
        public void GivenSupported16bitTransferSyntax_WhenRetrievingFrameAndAskingForConversion_ReturnedFileHasExpectedTransferSyntax(
            DicomTransferSyntax tsFrom,
            DicomTransferSyntax tsTo,
            PhotometricInterpretation photometricInterpretation)
        {
            EnsureArg.IsNotNull(photometricInterpretation, nameof(photometricInterpretation));
            EnsureArg.IsNotNull(tsFrom, nameof(tsFrom));
            EnsureArg.IsNotNull(tsTo, nameof(tsTo));
            DicomFile dicomFile = StreamAndStoredFileFromDataset(photometricInterpretation, false, tsFrom).Result.dicomFile;
            dicomFile.Dataset.ToInstanceIdentifier();

            _transcoder.TranscodeFrame(dicomFile, 1, tsTo.UID.UID);
        }

        public static IEnumerable<object[]> GetSupported8BitTranscoderCombos()
        {
            HashSet<DicomTransferSyntax> fromList = SupportedTransferSyntaxesFor8BitTranscoding;
            HashSet<DicomTransferSyntax> toList = SupportedTransferSyntaxesFor8BitTranscoding;
            HashSet<PhotometricInterpretation> photometricInterpretations = SupportedPhotometricInterpretations;

            HashSet<(DicomTransferSyntax fromTs, DicomTransferSyntax toTs, PhotometricInterpretation photometricInterpretation)> supported8BitTranscoderCombos =
                (from x in fromList from y in toList from z in photometricInterpretations select (x, y, z)).ToHashSet();

            supported8BitTranscoderCombos.ExceptWith(GenerateUnsupported8BitFromJPEG2000GeneratorCombos());
            supported8BitTranscoderCombos.ExceptWith(GenerateUnsupported8BitFromJPEG2000TranscoderCombos());
            supported8BitTranscoderCombos.ExceptWith(GenerateUnsupported8BitFromJPEGProcessGeneratorCombos());
            supported8BitTranscoderCombos.ExceptWith(GenerateUnsupported8BitToJPEGProcessTranscoderCombos());
            supported8BitTranscoderCombos.ExceptWith(GenerateUnsupported8BitToJPEGTranscoderCombos());
            supported8BitTranscoderCombos.ExceptWith(GenerateUnsupported8BitFromJPEGProcessMonochromePITranscoderCombos());

            return from x in supported8BitTranscoderCombos select new object[] { x.fromTs, x.toTs, x.photometricInterpretation };
        }

        public static IEnumerable<object[]> GetUnsupported8BitGeneratorCombos()
        {
            HashSet<(DicomTransferSyntax fromTs, DicomTransferSyntax toTs, PhotometricInterpretation photometricInterpretation)> supported8BitTranscoderCombos =
                new HashSet<(DicomTransferSyntax, DicomTransferSyntax, PhotometricInterpretation)>();

            supported8BitTranscoderCombos.UnionWith(GenerateUnsupported8BitFromJPEG2000GeneratorCombos());
            supported8BitTranscoderCombos.UnionWith(GenerateUnsupported8BitFromJPEGProcessGeneratorCombos());

            return from x in supported8BitTranscoderCombos select new object[] { x.fromTs, x.toTs, x.photometricInterpretation };
        }

        public static IEnumerable<object[]> GetUnsupported8BitTranscoderCombos()
        {
            HashSet<(DicomTransferSyntax fromTs, DicomTransferSyntax toTs, PhotometricInterpretation photometricInterpretation)> supported8BitTranscoderCombos =
                new HashSet<(DicomTransferSyntax, DicomTransferSyntax, PhotometricInterpretation)>();

            supported8BitTranscoderCombos.UnionWith(GenerateUnsupported8BitFromJPEG2000TranscoderCombos());
            supported8BitTranscoderCombos.UnionWith(GenerateUnsupported8BitToJPEGProcessTranscoderCombos());
            supported8BitTranscoderCombos.UnionWith(GenerateUnsupported8BitFromJPEGProcessMonochromePITranscoderCombos());

            return from x in supported8BitTranscoderCombos select new object[] { x.fromTs, x.toTs, x.photometricInterpretation };
        }

        public static IEnumerable<object[]> Get16BitTranscoderCombos()
        {
            HashSet<DicomTransferSyntax> fromList = SupportedTransferSyntaxesForOver8BitTranscoding;
            HashSet<DicomTransferSyntax> toList = SupportedTransferSyntaxesForOver8BitTranscoding;
            HashSet<PhotometricInterpretation> photometricInterpretations = SupportedPhotometricInterpretations;

            return from x in fromList from y in toList from z in photometricInterpretations select new object[] { x, y, z };
        }

        public static HashSet<(DicomTransferSyntax, DicomTransferSyntax, PhotometricInterpretation)> GenerateUnsupported8BitFromJPEG2000GeneratorCombos()
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

        public static HashSet<(DicomTransferSyntax, DicomTransferSyntax, PhotometricInterpretation)> GenerateUnsupported8BitFromJPEG2000TranscoderCombos()
        {
            HashSet<DicomTransferSyntax> fromTs = new HashSet<DicomTransferSyntax>
            {
                DicomTransferSyntax.JPEG2000Lossless,
                DicomTransferSyntax.JPEG2000Lossy,
            };
            HashSet<DicomTransferSyntax> toTs = new HashSet<DicomTransferSyntax>
            {
                DicomTransferSyntax.JPEG2000Lossless,
                DicomTransferSyntax.JPEG2000Lossy,
            };
            HashSet<PhotometricInterpretation> photometricInterpretations = new HashSet<PhotometricInterpretation>
            {
                PhotometricInterpretation.YbrIct,
                PhotometricInterpretation.YbrRct,
            };

            return (from x in fromTs from y in toTs from z in photometricInterpretations select (x, y, z)).ToHashSet();
        }

        public static HashSet<(DicomTransferSyntax, DicomTransferSyntax, PhotometricInterpretation)> GenerateUnsupported8BitFromJPEGProcessGeneratorCombos()
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

        public static HashSet<(DicomTransferSyntax, DicomTransferSyntax, PhotometricInterpretation)> GenerateUnsupported8BitToJPEGProcessTranscoderCombos()
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
                PhotometricInterpretation.YbrFull,
                PhotometricInterpretation.YbrIct,
                PhotometricInterpretation.YbrRct,
            };

            return (from x in fromTs from y in toTs from z in photometricInterpretations select (x, y, z)).ToHashSet();
        }

        public static HashSet<(DicomTransferSyntax, DicomTransferSyntax, PhotometricInterpretation)> GenerateUnsupported8BitToJPEGTranscoderCombos()
        {
            HashSet<DicomTransferSyntax> fromTs = new HashSet<DicomTransferSyntax>
            {
                DicomTransferSyntax.DeflatedExplicitVRLittleEndian,
                DicomTransferSyntax.ExplicitVRBigEndian,
                DicomTransferSyntax.ExplicitVRLittleEndian,
                DicomTransferSyntax.ImplicitVRLittleEndian,
                DicomTransferSyntax.RLELossless,
            };
            HashSet<DicomTransferSyntax> toTs = new HashSet<DicomTransferSyntax>
            {
                DicomTransferSyntax.JPEG2000Lossless,
                DicomTransferSyntax.JPEG2000Lossy,
                DicomTransferSyntax.JPEGProcess1,
                DicomTransferSyntax.JPEGProcess2_4,
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

        public static HashSet<(DicomTransferSyntax, DicomTransferSyntax, PhotometricInterpretation)> GenerateUnsupported8BitFromJPEGProcessMonochromePITranscoderCombos()
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
