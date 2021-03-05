// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="DicomTag"/>.
    /// </summary>
    public static class DicomElementExtensions
    {
        private static readonly IReadOnlyDictionary<DicomVR, Func<DicomElement, object>> Readers = new Dictionary<DicomVR, Func<DicomElement, object>>()
        {
            { DicomVR.AE, GetStringValue },
            { DicomVR.AS, GetStringValue },
            { DicomVR.CS, GetStringValue },
            { DicomVR.DA, GetDateTimeValue },
            { DicomVR.DS, GetStringValue },
            { DicomVR.DT, GetDateTimeValue },
            { DicomVR.FL, GetFloatValue },
            { DicomVR.FD, GetDoubleValue },
            { DicomVR.IS, GetStringValue },
            { DicomVR.LO, GetStringValue },
            { DicomVR.PN, GetStringValue },
            { DicomVR.SH, GetStringValue },
            { DicomVR.SL, GetIntValue },
            { DicomVR.SS, GetShortValue },
            { DicomVR.TM, GetDateTimeValue },
            { DicomVR.UI, GetStringValue },
            { DicomVR.UL, GetUIntValue },
            { DicomVR.US, GetUShortValue },
        };

        public static object GetValue(this DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            if (!Readers.ContainsKey(element.ValueRepresentation))
            {
                Debug.Fail($"Need reader for VR {element.ValueRepresentation}");
            }

            return Readers[element.ValueRepresentation].Invoke(element);
        }

        private static object GetStringValue(DicomElement element)
        {
            return element.Get<string>();
        }

        private static object GetDateTimeValue(DicomElement element)
        {
            // TODO: need same way as min validator
            return element.Get<DateTime>();
        }

        private static object GetFloatValue(DicomElement element)
        {
            return element.Get<float>();
        }

        private static object GetDoubleValue(DicomElement element)
        {
            return element.Get<double>();
        }

        private static object GetIntValue(DicomElement element)
        {
            return element.Get<int>();
        }

        private static object GetShortValue(DicomElement element)
        {
            return element.Get<short>();
        }

        private static object GetUIntValue(DicomElement element)
        {
            return element.Get<uint>();
        }

        private static object GetUShortValue(DicomElement element)
        {
            return element.Get<ushort>();
        }
    }
}
