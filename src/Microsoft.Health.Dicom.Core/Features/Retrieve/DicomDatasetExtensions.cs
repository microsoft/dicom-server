// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Dicom.Imaging;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public static class DicomDatasetExtensions
    {
        private static readonly HashSet<DicomTransferSyntax> SupportedTransferSyntaxes8Bit = new HashSet<DicomTransferSyntax>
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

        private static readonly HashSet<DicomTransferSyntax> SupportedTransferSyntaxesOver8Bit = new HashSet<DicomTransferSyntax>
        {
            DicomTransferSyntax.DeflatedExplicitVRLittleEndian,
            DicomTransferSyntax.ExplicitVRBigEndian,
            DicomTransferSyntax.ExplicitVRLittleEndian,
            DicomTransferSyntax.ImplicitVRLittleEndian,
            DicomTransferSyntax.RLELossless,
        };

        public static bool CanTranscodeDataset(this DicomDataset ds, DicomTransferSyntax toTransferSyntax)
        {
            EnsureArg.IsNotNull(ds, nameof(ds));

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

            if ((fromTs == DicomTransferSyntax.JPEG2000Lossless || fromTs == DicomTransferSyntax.JPEG2000Lossy) &&
                ((photometricInterpretation == PhotometricInterpretation.Rgb.Value) ||
                 (photometricInterpretation == PhotometricInterpretation.YbrFull422.Value) ||
                 (photometricInterpretation == PhotometricInterpretation.YbrPartial422.Value) ||
                 (photometricInterpretation == PhotometricInterpretation.YbrPartial420.Value)))
            {
                return false;
            }

            if ((fromTs == DicomTransferSyntax.JPEGProcess1 || fromTs == DicomTransferSyntax.JPEGProcess2_4) &&
                ((photometricInterpretation == PhotometricInterpretation.Rgb.Value) ||
                 (photometricInterpretation == PhotometricInterpretation.YbrFull.Value) ||
                 (photometricInterpretation == PhotometricInterpretation.YbrFull422.Value) ||
                 (photometricInterpretation == PhotometricInterpretation.YbrPartial422.Value) ||
                 (photometricInterpretation == PhotometricInterpretation.YbrPartial420.Value) ||
                 (photometricInterpretation == PhotometricInterpretation.YbrIct.Value) ||
                 (photometricInterpretation == PhotometricInterpretation.YbrRct.Value)))
            {
                return false;
            }

            // Bug in fo-dicom 4.0.1
            if ((toTransferSyntax == DicomTransferSyntax.JPEGProcess1 || toTransferSyntax == DicomTransferSyntax.JPEGProcess2_4) &&
                ((photometricInterpretation == PhotometricInterpretation.Monochrome1.Value) ||
                 (photometricInterpretation == PhotometricInterpretation.Monochrome2.Value)))
            {
                return false;
            }

            if (((bpp > 8) && SupportedTransferSyntaxesOver8Bit.Contains(toTransferSyntax) && SupportedTransferSyntaxesOver8Bit.Contains(fromTs)) ||
                ((bpp <= 8) && SupportedTransferSyntaxes8Bit.Contains(toTransferSyntax) && SupportedTransferSyntaxes8Bit.Contains(fromTs)))
            {
                return true;
            }

            return false;
        }
    }
}
