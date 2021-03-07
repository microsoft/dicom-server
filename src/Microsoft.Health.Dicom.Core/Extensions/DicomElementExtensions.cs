// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="DicomElement"/>.
    /// </summary>
    public static class DicomElementExtensions
    {
        private static readonly IReadOnlyDictionary<DicomVR, Func<DicomElement, object>> Readers = new Dictionary<DicomVR, Func<DicomElement, object>>()
        {
            { DicomVR.AE, GetStringValue },
            { DicomVR.AS, GetStringValue },
            { DicomVR.AT, GetDicomTagAsULongValue },
            { DicomVR.CS, GetStringValue },
            { DicomVR.DA, GetDAValue },
            { DicomVR.DS, GetStringValue },
            { DicomVR.DT, GetDTValue },
            { DicomVR.FL, GetFloatValue },
            { DicomVR.FD, GetDoubleValue },
            { DicomVR.IS, GetStringValue },
            { DicomVR.LO, GetStringValue },
            { DicomVR.PN, GetStringValue },
            { DicomVR.SH, GetStringValue },
            { DicomVR.SL, GetIntValue },
            { DicomVR.SS, GetShortValue },
            { DicomVR.TM, GetTMValue },
            { DicomVR.UI, GetStringValue },
            { DicomVR.UL, GetUIntValue },
            { DicomVR.US, GetUShortValue },
        };

        /// <summary>
        /// Get value of DicomElement.
        /// </summary>
        /// <remarks>VM of the DicomElement must be 1. Value rather than 1 will return null.</remarks>
        /// <param name="element">The DicomElement</param>
        /// <returns>The value.</returns>
        public static object GetSingleValue(this DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            if (element.Count != 1)
            {
                return null;
            }

            if (!Readers.ContainsKey(element.ValueRepresentation))
            {
                Debug.Fail($"Need reader for VR {element.ValueRepresentation}");
                return null;
            }

            return Readers[element.ValueRepresentation].Invoke(element);
        }

        private static object GetDAValue(DicomElement element)
        {
            string stringDate = (string)SafeGetValue<string>(element);

            if (stringDate == null ||
                !DicomElementMinimumValidation.TryParseDA(stringDate, out DateTime result))
            {
                return null;
            }

            return result;
        }

        private static object SafeGetValue<T>(DicomElement element)
        {
            try
            {
                return element.Get<T>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        private static object GetDTValue(DicomElement element)
        {
            string stringDate = (string)SafeGetValue<string>(element);

            if (stringDate == null ||
                !DicomElementMinimumValidation.TryParseDT(stringDate, out DateTime result))
            {
                return null;
            }

            return result;
        }

        private static object GetTMValue(DicomElement element)
        {
            string stringDate = (string)SafeGetValue<string>(element);

            if (stringDate == null ||
                !DicomElementMinimumValidation.TryParseTM(stringDate, out DateTime result))
            {
                return null;
            }

            return result;
        }

        private static object GetStringValue(DicomElement element)
        {
            return SafeGetValue<string>(element);
        }

        private static object GetFloatValue(DicomElement element)
        {
            return SafeGetValue<float>(element);
        }

        private static object GetDoubleValue(DicomElement element)
        {
            return SafeGetValue<double>(element);
        }

        private static object GetIntValue(DicomElement element)
        {
            return SafeGetValue<int>(element);
        }

        private static object GetShortValue(DicomElement element)
        {
            return SafeGetValue<short>(element);
        }

        private static object GetUIntValue(DicomElement element)
        {
            return SafeGetValue<uint>(element);
        }

        private static object GetDicomTagAsULongValue(DicomElement element)
        {
            DicomTag tag = (DicomTag)SafeGetValue<DicomTag>(element);
            if (tag != null)
            {
                return (ulong)(tag.Group << 16) + tag.Element;
            }

            return null;
        }

        private static object GetUShortValue(DicomElement element)
        {
            return SafeGetValue<ushort>(element);
        }
    }
}
