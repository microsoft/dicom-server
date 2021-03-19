// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.SqlServer.Features.CustomTag
{
    /// <summary>
    /// Build AddInstanceTableValuedParameters
    /// </summary>
    internal static class AddInstanceTableValuedParametersBuilder
    {
        private static readonly Dictionary<DicomVR, Func<DicomDataset, DicomTag, DateTime?>> DataTimeReaders = new Dictionary<DicomVR, Func<DicomDataset, DicomTag, DateTime?>>()
        {
            { DicomVR.DA, Core.Extensions.DicomDatasetExtensions.GetStringDateAsDate },
        };

        /// <summary>
        /// Read Index Tag values from DicomDataset.
        /// </summary>
        /// <param name="instance">The dicom dataset.</param>
        /// <param name="indexTags">The index tags.</param>
        public static VLatest.AddInstanceTableValuedParameters Build(
            DicomDataset instance,
            IEnumerable<IndexTag> indexTags)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(indexTags, nameof(indexTags));

            List<InsertStringCustomTagTableTypeV1Row> stringRows = new List<InsertStringCustomTagTableTypeV1Row>();
            List<InsertBigIntCustomTagTableTypeV1Row> bigIntRows = new List<InsertBigIntCustomTagTableTypeV1Row>();
            List<InsertDoubleCustomTagTableTypeV1Row> doubleRows = new List<InsertDoubleCustomTagTableTypeV1Row>();
            List<InsertDateTimeCustomTagTableTypeV1Row> dateTimeRows = new List<InsertDateTimeCustomTagTableTypeV1Row>();
            List<InsertPersonNameCustomTagTableTypeV1Row> personNamRows = new List<InsertPersonNameCustomTagTableTypeV1Row>();

            foreach (var indexTag in indexTags)
            {
                CustomTagDataType dataType = CustomTagLimit.CustomTagVRAndDataTypeMapping[indexTag.VR.Code];
                switch (dataType)
                {
                    case CustomTagDataType.StringData:
                        AddStringRow(instance, stringRows, indexTag);

                        break;

                    case CustomTagDataType.LongData:
                        AddBigIntRow(instance, bigIntRows, indexTag);

                        break;

                    case CustomTagDataType.DoubleData:
                        AddDoubleRow(instance, doubleRows, indexTag);

                        break;

                    case CustomTagDataType.DateTimeData:
                        AddDateTimeRow(instance, dateTimeRows, indexTag);

                        break;

                    case CustomTagDataType.PersonNameData:
                        AddPersonNameRow(instance, personNamRows, indexTag);

                        break;

                    default:
                        Debug.Fail($"Not able to handle {dataType}");
                        break;
                }
            }

            return new VLatest.AddInstanceTableValuedParameters(stringRows, bigIntRows, doubleRows, dateTimeRows, personNamRows);
        }

        private static void AddPersonNameRow(DicomDataset instance, List<InsertPersonNameCustomTagTableTypeV1Row> personNamRows, IndexTag indexTag)
        {
            string personNameVal = instance.GetSingleValueOrDefault<string>(indexTag.Tag);
            if (personNameVal != null)
            {
                personNamRows.Add(new InsertPersonNameCustomTagTableTypeV1Row(indexTag.CustomTagStoreEntry.Key, personNameVal, (byte)indexTag.Level));
            }
        }

        private static void AddDateTimeRow(DicomDataset instance, List<InsertDateTimeCustomTagTableTypeV1Row> dateTimeRows, IndexTag indexTag)
        {
            DateTime? dateVal = DataTimeReaders.TryGetValue(
                             indexTag.VR,
                             out Func<DicomDataset, DicomTag, DateTime?> reader) ? reader.Invoke(instance, indexTag.Tag) : null;

            if (dateVal.HasValue)
            {
                dateTimeRows.Add(new InsertDateTimeCustomTagTableTypeV1Row(indexTag.CustomTagStoreEntry.Key, dateVal.Value, (byte)indexTag.Level));
            }
        }

        private static void AddDoubleRow(DicomDataset instance, List<InsertDoubleCustomTagTableTypeV1Row> doubleRows, IndexTag indexTag)
        {
            double? doubleVal = instance.GetSingleValueOrDefault<double>(indexTag.Tag);
            if (doubleVal.HasValue)
            {
                doubleRows.Add(new InsertDoubleCustomTagTableTypeV1Row(indexTag.CustomTagStoreEntry.Key, doubleVal.Value, (byte)indexTag.Level));
            }
        }

        private static void AddBigIntRow(DicomDataset instance, List<InsertBigIntCustomTagTableTypeV1Row> bigIntRows, IndexTag indexTag)
        {
            long? longVal = instance.GetSingleValueOrDefault<long>(indexTag.Tag);

            if (longVal.HasValue)
            {
                bigIntRows.Add(new InsertBigIntCustomTagTableTypeV1Row(indexTag.CustomTagStoreEntry.Key, longVal.Value, (byte)indexTag.Level));
            }
        }

        private static void AddStringRow(DicomDataset instance, List<InsertStringCustomTagTableTypeV1Row> stringRows, IndexTag indexTag)
        {
            string stringVal = instance.GetSingleValueOrDefault<string>(indexTag.Tag);
            if (stringVal != null)
            {
                stringRows.Add(new InsertStringCustomTagTableTypeV1Row(indexTag.CustomTagStoreEntry.Key, stringVal, (byte)indexTag.Level));
            }
        }
    }
}
