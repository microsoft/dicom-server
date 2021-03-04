// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using Dicom;

namespace Microsoft.Health.Dicom.SqlServer.Features.CustomTag
{
    /// <summary>
    /// Limits on CustomTag feature.
    /// </summary>
    internal static class CustomTagLimit
    {
        /// <summary>
        /// Mapping from CustomTagVR to DateType
        /// </summary>
        public static readonly IReadOnlyDictionary<string, CustomTagDataType> CustomTagVRAndDataTypeMapping = new Dictionary<string, CustomTagDataType>()
        {
            { DicomVR.AE.Code, CustomTagDataType.StringData },
            { DicomVR.AS.Code, CustomTagDataType.StringData },
            { DicomVR.AT.Code, CustomTagDataType.LongData },
            { DicomVR.CS.Code, CustomTagDataType.StringData },
            { DicomVR.DA.Code, CustomTagDataType.DateTimeData },
            { DicomVR.DS.Code, CustomTagDataType.StringData },
            { DicomVR.DT.Code, CustomTagDataType.DateTimeData },
            { DicomVR.FL.Code, CustomTagDataType.DoubleData },
            { DicomVR.FD.Code, CustomTagDataType.DoubleData },
            { DicomVR.IS.Code, CustomTagDataType.StringData },
            { DicomVR.LO.Code, CustomTagDataType.StringData },
            { DicomVR.PN.Code, CustomTagDataType.PersonNameData },
            { DicomVR.SH.Code, CustomTagDataType.StringData },
            { DicomVR.SL.Code, CustomTagDataType.LongData },
            { DicomVR.SS.Code, CustomTagDataType.LongData },
            { DicomVR.TM.Code, CustomTagDataType.DateTimeData },
            { DicomVR.UI.Code, CustomTagDataType.StringData },
            { DicomVR.UL.Code, CustomTagDataType.LongData },
            { DicomVR.US.Code, CustomTagDataType.LongData },
        };
    }
}
