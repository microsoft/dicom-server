// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using Dicom;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag
{
    /// <summary>
    /// Limits on ExtendedQueryTag feature.
    /// </summary>
    internal static class ExtendedQueryTagLimit
    {
        /// <summary>
        /// Mapping from ExtendedQueryTagVR to DateType
        /// </summary>
        public static readonly IReadOnlyDictionary<string, ExtendedQueryTagDataType> ExtendedQueryTagVRAndDataTypeMapping = new Dictionary<string, ExtendedQueryTagDataType>()
        {
            { DicomVR.AE.Code, ExtendedQueryTagDataType.StringData },
            { DicomVR.AS.Code, ExtendedQueryTagDataType.StringData },
            { DicomVR.CS.Code, ExtendedQueryTagDataType.StringData },
            { DicomVR.DA.Code, ExtendedQueryTagDataType.DateTimeData },
            { DicomVR.DS.Code, ExtendedQueryTagDataType.StringData },
            { DicomVR.FL.Code, ExtendedQueryTagDataType.DoubleData },
            { DicomVR.FD.Code, ExtendedQueryTagDataType.DoubleData },
            { DicomVR.IS.Code, ExtendedQueryTagDataType.StringData },
            { DicomVR.LO.Code, ExtendedQueryTagDataType.StringData },
            { DicomVR.PN.Code, ExtendedQueryTagDataType.PersonNameData },
            { DicomVR.SH.Code, ExtendedQueryTagDataType.StringData },
            { DicomVR.SL.Code, ExtendedQueryTagDataType.LongData },
            { DicomVR.SS.Code, ExtendedQueryTagDataType.LongData },
            { DicomVR.UI.Code, ExtendedQueryTagDataType.StringData },
            { DicomVR.UL.Code, ExtendedQueryTagDataType.LongData },
            { DicomVR.US.Code, ExtendedQueryTagDataType.LongData },
        };
    }
}
