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
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag
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
            IEnumerable<QueryTag> indexTags)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(indexTags, nameof(indexTags));

            List<InsertStringExtendedQueryTagTableTypeV1Row> stringRows = new List<InsertStringExtendedQueryTagTableTypeV1Row>();
            List<InsertBigIntExtendedQueryTagTableTypeV1Row> bigIntRows = new List<InsertBigIntExtendedQueryTagTableTypeV1Row>();
            List<InsertDoubleExtendedQueryTagTableTypeV1Row> doubleRows = new List<InsertDoubleExtendedQueryTagTableTypeV1Row>();
            List<InsertDateTimeExtendedQueryTagTableTypeV1Row> dateTimeRows = new List<InsertDateTimeExtendedQueryTagTableTypeV1Row>();
            List<InsertPersonNameExtendedQueryTagTableTypeV1Row> personNamRows = new List<InsertPersonNameExtendedQueryTagTableTypeV1Row>();

            foreach (var indexTag in indexTags)
            {
                ExtendedQueryTagDataType dataType = ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[indexTag.VR.Code];
                switch (dataType)
                {
                    case ExtendedQueryTagDataType.StringData:
                        AddStringRow(instance, stringRows, indexTag);

                        break;

                    case ExtendedQueryTagDataType.LongData:
                        AddBigIntRow(instance, bigIntRows, indexTag);

                        break;

                    case ExtendedQueryTagDataType.DoubleData:
                        AddDoubleRow(instance, doubleRows, indexTag);

                        break;

                    case ExtendedQueryTagDataType.DateTimeData:
                        AddDateTimeRow(instance, dateTimeRows, indexTag);

                        break;

                    case ExtendedQueryTagDataType.PersonNameData:
                        AddPersonNameRow(instance, personNamRows, indexTag);

                        break;

                    default:
                        Debug.Fail($"Not able to handle {dataType}");
                        break;
                }
            }

            return new VLatest.AddInstanceTableValuedParameters(stringRows, bigIntRows, doubleRows, dateTimeRows, personNamRows);
        }

        private static void AddPersonNameRow(DicomDataset instance, List<InsertPersonNameExtendedQueryTagTableTypeV1Row> personNamRows, QueryTag indexTag)
        {
            string personNameVal = instance.GetSingleValueOrDefault<string>(indexTag.Tag);
            if (personNameVal != null)
            {
                personNamRows.Add(new InsertPersonNameExtendedQueryTagTableTypeV1Row(indexTag.ExtendedQueryTagStoreEntry.Key, personNameVal, (byte)indexTag.Level));
            }
        }

        private static void AddDateTimeRow(DicomDataset instance, List<InsertDateTimeExtendedQueryTagTableTypeV1Row> dateTimeRows, QueryTag indexTag)
        {
            DateTime? dateVal = DataTimeReaders.TryGetValue(
                             indexTag.VR,
                             out Func<DicomDataset, DicomTag, DateTime?> reader) ? reader.Invoke(instance, indexTag.Tag) : null;

            if (dateVal.HasValue)
            {
                dateTimeRows.Add(new InsertDateTimeExtendedQueryTagTableTypeV1Row(indexTag.ExtendedQueryTagStoreEntry.Key, dateVal.Value, (byte)indexTag.Level));
            }
        }

        private static void AddDoubleRow(DicomDataset instance, List<InsertDoubleExtendedQueryTagTableTypeV1Row> doubleRows, QueryTag indexTag)
        {
            double? doubleVal = instance.GetSingleValueOrDefault<double>(indexTag.Tag);
            if (doubleVal.HasValue)
            {
                doubleRows.Add(new InsertDoubleExtendedQueryTagTableTypeV1Row(indexTag.ExtendedQueryTagStoreEntry.Key, doubleVal.Value, (byte)indexTag.Level));
            }
        }

        private static void AddBigIntRow(DicomDataset instance, List<InsertBigIntExtendedQueryTagTableTypeV1Row> bigIntRows, QueryTag indexTag)
        {
            long? longVal = instance.GetSingleValueOrDefault<long>(indexTag.Tag);

            if (longVal.HasValue)
            {
                bigIntRows.Add(new InsertBigIntExtendedQueryTagTableTypeV1Row(indexTag.ExtendedQueryTagStoreEntry.Key, longVal.Value, (byte)indexTag.Level));
            }
        }

        private static void AddStringRow(DicomDataset instance, List<InsertStringExtendedQueryTagTableTypeV1Row> stringRows, QueryTag indexTag)
        {
            string stringVal = instance.GetSingleValueOrDefault<string>(indexTag.Tag);
            if (stringVal != null)
            {
                stringRows.Add(new InsertStringExtendedQueryTagTableTypeV1Row(indexTag.ExtendedQueryTagStoreEntry.Key, stringVal, (byte)indexTag.Level));
            }
        }
    }
}
