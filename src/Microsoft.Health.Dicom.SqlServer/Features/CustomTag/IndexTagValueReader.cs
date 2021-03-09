// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Dicom.SqlServer.Features.CustomTag
{
    /// <summary>
    /// Date type of custom tag.
    /// </summary>
    internal static class IndexTagValueReader
    {
        public static void Read(
            DicomDataset instance,
            IEnumerable<IndexTag> indexTags,
            out IDictionary<IndexTag, string> stringValues,
            out IDictionary<IndexTag, long> longValues,
            out IDictionary<IndexTag, double> doubleValues,
            out IDictionary<IndexTag, DateTime> datetimeValues,
            out IDictionary<IndexTag, string> personNameValues)
        {
            stringValues = new Dictionary<IndexTag, string>();
            longValues = new Dictionary<IndexTag, long>();
            doubleValues = new Dictionary<IndexTag, double>();
            datetimeValues = new Dictionary<IndexTag, DateTime>();
            personNameValues = new Dictionary<IndexTag, string>();

            var customTags = indexTags.Where(tag => tag.IsCustomTag);
            var tags = instance.GetDicomTags(customTags);

            foreach (var pair in tags)
            {
                DicomTag tag = pair.Value;
                IndexTag indexTag = pair.Key;

                CustomTagDataType dataType = CustomTagLimit.CustomTagVRAndDataTypeMapping[indexTag.VR.Code];
                switch (dataType)
                {
                    case CustomTagDataType.StringData:
                        AddStringValue(instance, stringValues, tag, indexTag);
                        break;
                    case CustomTagDataType.LongData:
                        AddLongValue(instance, longValues, tag, indexTag);

                        break;
                    case CustomTagDataType.DoubleData:
                        AddDoubleValue(instance, doubleValues, tag, indexTag);

                        break;
                    case CustomTagDataType.DateTimeData:
                        AddDateTimeValue(instance, datetimeValues, tag, indexTag);

                        break;
                    case CustomTagDataType.PersonNameData:
                        AddPersonNameValue(instance, personNameValues, tag, indexTag);

                        break;
                    default:
                        Debug.Fail($"Not able to handle {dataType}");
                        break;
                }
            }
        }

        private static void AddPersonNameValue(DicomDataset instance, IDictionary<IndexTag, string> personNameValues, DicomTag tag, IndexTag indexTag)
        {
            string personNameVal = instance.GetSingleValueOrDefault<string>(tag);
            if (personNameVal != null)
            {
                personNameValues.Add(indexTag, personNameVal);
            }
        }

        private static void AddDateTimeValue(DicomDataset instance, IDictionary<IndexTag, DateTime> datetimeValues, DicomTag tag, IndexTag indexTag)
        {
            DateTime? dateVal = null;
            if (indexTag.VR == DicomVR.DA)
            {
                dateVal = instance.GetStringDateAsDate(tag);
            }
            else if (indexTag.VR == DicomVR.DT)
            {
                dateVal = instance.GetStringDateAsDateTime(tag);
            }
            else if (indexTag.VR == DicomVR.TM)
            {
                dateVal = instance.GetStringDateAsTime(tag);
            }
            else
            {
                Debug.Fail($"{indexTag.VR} is not supported.");
            }

            if (dateVal.HasValue)
            {
                datetimeValues.Add(indexTag, dateVal.Value);
            }
        }

        private static void AddDoubleValue(DicomDataset instance, IDictionary<IndexTag, double> doubleValues, DicomTag tag, IndexTag indexTag)
        {
            double? doubleVal = instance.GetSingleValueOrDefault<double>(tag);
            if (doubleVal.HasValue)
            {
                doubleValues.Add(indexTag, doubleVal.Value);
            }
        }

        private static void AddLongValue(DicomDataset instance, IDictionary<IndexTag, long> longValues, DicomTag tag, IndexTag indexTag)
        {
            long? longVal;
            if (indexTag.VR == DicomVR.AT)
            {
                longVal = instance.GetDicomTagValueAsLong(tag);
            }
            else
            {
                longVal = instance.GetSingleValueOrDefault<long>(tag);
            }

            if (longVal.HasValue)
            {
                longValues.Add(indexTag, longVal.Value);
            }
        }

        private static void AddStringValue(DicomDataset instance, IDictionary<IndexTag, string> stringValues, DicomTag tag, IndexTag indexTag)
        {
            string stringVal = instance.GetSingleValueOrDefault<string>(tag);
            if (stringVal != null)
            {
                stringValues.Add(indexTag, stringVal);
            }
        }
    }
}
