// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    internal static class ValidationLimits
    {
        public static readonly IReadOnlyDictionary<DicomVR, DicomVRType> SupportedVRs = new Dictionary<DicomVR, DicomVRType>
        {
            { DicomVR.AE,   DicomVRType.Text },
            { DicomVR.AS,   DicomVRType.Text },
            { DicomVR.CS,   DicomVRType.Text },
            { DicomVR.DA,   DicomVRType.Text },
            { DicomVR.DS,   DicomVRType.Text },
            { DicomVR.IS,   DicomVRType.Text },
            { DicomVR.LO,   DicomVRType.Text },
            { DicomVR.PN,   DicomVRType.Text },
            { DicomVR.SH,   DicomVRType.Text },
            { DicomVR.UI,   DicomVRType.Text },
            { DicomVR.FL,   DicomVRType.Binary },
            { DicomVR.FD,   DicomVRType.Binary },
            { DicomVR.SL,   DicomVRType.Binary },
            { DicomVR.SS,   DicomVRType.Binary },
            {  DicomVR.UL,  DicomVRType.Binary },
            { DicomVR.US,   DicomVRType.Binary },
        };
    }
}
