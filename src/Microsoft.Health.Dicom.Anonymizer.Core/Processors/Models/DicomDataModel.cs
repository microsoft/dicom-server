// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class DicomDataModel
    {
        public static HashSet<DicomVR> CryptoHashSupportedVR { get; } = new HashSet<DicomVR>()
        {
            DicomVR.AE,
            DicomVR.AS,
            DicomVR.CS,
            DicomVR.UI,
            DicomVR.DS,
            DicomVR.IS,
            DicomVR.SH,
            DicomVR.DA,
            DicomVR.DT,
            DicomVR.TM,
            DicomVR.PN,
            DicomVR.UC,
            DicomVR.LO,
            DicomVR.UT,
            DicomVR.ST,
            DicomVR.LT,
            DicomVR.UR,
            DicomVR.OB,
            DicomVR.UN,
        };

        public static HashSet<DicomVR> EncryptSupportedVR { get; } = new HashSet<DicomVR>()
        {
            DicomVR.AE,
            DicomVR.AS,
            DicomVR.CS,
            DicomVR.UI,
            DicomVR.DS,
            DicomVR.IS,
            DicomVR.SH,
            DicomVR.DA,
            DicomVR.DT,
            DicomVR.TM,
            DicomVR.PN,
            DicomVR.UC,
            DicomVR.LO,
            DicomVR.UT,
            DicomVR.ST,
            DicomVR.LT,
            DicomVR.UR,
            DicomVR.OB,
            DicomVR.UN,
        };

        public static HashSet<DicomVR> DateShiftSupportedVR { get; } = new HashSet<DicomVR>()
        {
            DicomVR.DA,
            DicomVR.DT,
        };

        public static HashSet<DicomVR> PerturbSupportedVR { get; } = new HashSet<DicomVR>()
        {
            DicomVR.AS,
            DicomVR.DS,
            DicomVR.FL,
            DicomVR.OF,
            DicomVR.FD,
            DicomVR.OD,
            DicomVR.IS,
            DicomVR.SL,
            DicomVR.SS,
            DicomVR.US,
            DicomVR.OW,
            DicomVR.UL,
            DicomVR.OL,
            DicomVR.UV,
            DicomVR.OV,
            DicomVR.SV,
        };

        public static HashSet<DicomVR> RefreshUIDSupportedVR { get; } = new HashSet<DicomVR>()
        {
            DicomVR.UI,
        };
    }
}
