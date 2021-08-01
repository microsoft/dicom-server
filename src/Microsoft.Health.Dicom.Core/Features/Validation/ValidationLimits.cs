// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    internal static class ValidationLimits
    {
        private static readonly HashSet<DicomVR> StringVrs = new HashSet<DicomVR>()
        {
           DicomVR.AE,
           DicomVR.AS,
           DicomVR.CS,
           DicomVR.DA,
           DicomVR.DS,
           DicomVR.IS,
           DicomVR.LO,
           DicomVR.PN,
           DicomVR.SH,
           DicomVR.UI,
        };

        private static readonly HashSet<DicomVR> BinaryVrs = new HashSet<DicomVR>()
        {
            DicomVR.FL,
            DicomVR.FD,
            DicomVR.SL,
            DicomVR.SS,
            DicomVR.UL,
            DicomVR.US,
        };

        public static readonly HashSet<DicomVR> SupportedVRs = new HashSet<DicomVR>(StringVrs.Union(BinaryVrs));

        public static bool CanGetAsString(this DicomVR vr)
        {
            EnsureArg.IsNotNull(vr, nameof(vr));
            return StringVrs.Contains(vr);
        }
    }
}
