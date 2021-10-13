// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag
{
    /// <summary>
    /// Class that build ExtendedQueryTagRows.
    /// </summary>
    internal static class ExtendedQueryTagDataRowsBuilder

    {
        private static readonly Dictionary<DicomVR, Func<DicomDataset, DicomTag, DicomVR, DateTime?>> DateReaders = new Dictionary<DicomVR, Func<DicomDataset, DicomTag, DicomVR, DateTime?>>()
        {
            { DicomVR.DA, Core.Extensions.DicomDatasetExtensions.GetStringDateAsDate }
        };

        private static readonly Dictionary<DicomVR, Func<DicomDataset, DicomTag, DicomVR, Tuple<DateTime?, DateTime?>>> DateTimeReaders = new Dictionary<DicomVR, Func<DicomDataset, DicomTag, DicomVR, Tuple<DateTime?, DateTime?>>>()
        {
            { DicomVR.DT, Core.Extensions.DicomDatasetExtensions.GetStringDateTimeAsLiteralAndUtcDateTimes }
        };

        private static readonly Dictionary<DicomVR, Func<DicomDataset, DicomTag, DicomVR, long?>> LongReaders = new Dictionary<DicomVR, Func<DicomDataset, DicomTag, DicomVR, long?>>()
        {
            { DicomVR.TM, Core.Extensions.DicomDatasetExtensions.GetStringTimeAsLong }
        };

        public static ExtendedQueryTagDataRows Build(
            DicomDataset instance,
            IEnumerable<QueryTag> queryTags,
            SchemaVersion schemaVersion)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            var stringRows = new List<InsertStringExtendedQueryTagTableTypeV1Row>();
            var longRows = new List<InsertLongExtendedQueryTagTableTypeV1Row>();
            var doubleRows = new List<InsertDoubleExtendedQueryTagTableTypeV1Row>();
            var dateTimeRows = new List<InsertDateTimeExtendedQueryTagTableTypeV1Row>();
            var dateTimeWithUtcRows = new List<InsertDateTimeExtendedQueryTagTableTypeV2Row>();
            var personNameRows = new List<InsertPersonNameExtendedQueryTagTableTypeV1Row>();

            foreach (QueryTag queryTag in queryTags.Where(x => x.IsExtendedQueryTag))
            {
                // Create row
                ExtendedQueryTagDataType dataType = ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[queryTag.VR.Code];
                switch (dataType)
                {
                    case ExtendedQueryTagDataType.StringData: AddStringRow(instance, stringRows, queryTag); break;
                    case ExtendedQueryTagDataType.LongData: AddLongRow(instance, longRows, queryTag); break;
                    case ExtendedQueryTagDataType.DoubleData: AddDoubleRow(instance, doubleRows, queryTag); break;
                    case ExtendedQueryTagDataType.DateTimeData:
                        if ((int)schemaVersion < SchemaVersionConstants.SupportDTAndTMInExtendedQueryTagSchemaVersion)
                        {
                            AddDateTimeRow(instance, dateTimeRows, queryTag);
                        }
                        else
                        {
                            AddDateTimeWithUtcRow(instance, dateTimeWithUtcRows, queryTag);
                        }
                        break;
                    case ExtendedQueryTagDataType.PersonNameData: AddPersonNameRow(instance, personNameRows, queryTag); break;
                    default:
                        Debug.Fail($"Not able to handle {dataType}");
                        break;
                }
            }

            return new ExtendedQueryTagDataRows
            {
                StringRows = stringRows,
                LongRows = longRows,
                DoubleRows = doubleRows,
                DateTimeRows = dateTimeRows,
                DateTimeWithUtcRows = dateTimeWithUtcRows,
                PersonNameRows = personNameRows,
            };
        }

        public static int GetMaxTagKey(IEnumerable<QueryTag> queryTags)
        {
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            int max = 0;
            foreach (QueryTag tag in queryTags.Where(x => x.IsExtendedQueryTag))
            {
                max = Math.Max(max, tag.ExtendedQueryTagStoreEntry.Key);
            }

            return max;
        }

        private static void AddPersonNameRow(DicomDataset instance, List<InsertPersonNameExtendedQueryTagTableTypeV1Row> personNamRows, QueryTag queryTag)
        {
            string personNameVal = instance.GetSingleValueOrDefault<string>(queryTag.Tag, expectedVR: queryTag.VR);
            if (personNameVal != null)
            {
                personNamRows.Add(new InsertPersonNameExtendedQueryTagTableTypeV1Row(queryTag.ExtendedQueryTagStoreEntry.Key, personNameVal, (byte)queryTag.Level));
            }
        }

        private static void AddDateTimeRow(DicomDataset instance, List<InsertDateTimeExtendedQueryTagTableTypeV1Row> dateTimeRows, QueryTag queryTag)
        {
            DateTime? dateVal = null;

            switch (queryTag.VR.Code)
            {
                case "DT":
                    dateVal = DateTimeReaders.TryGetValue(
                                queryTag.VR,
                                out Func<DicomDataset, DicomTag, DicomVR, Tuple<DateTime?, DateTime?>> dateTimeReader) ? dateTimeReader.Invoke(instance, queryTag.Tag, queryTag.VR).Item1 : null;
                    break;
                case "DA":
                default:
                    dateVal = DateReaders.TryGetValue(
                                    queryTag.VR,
                                    out Func<DicomDataset, DicomTag, DicomVR, DateTime?> reader) ? reader.Invoke(instance, queryTag.Tag, queryTag.VR) : null;
                    break;
            }

            if (dateVal.HasValue)
            {
                dateTimeRows.Add(new InsertDateTimeExtendedQueryTagTableTypeV1Row(queryTag.ExtendedQueryTagStoreEntry.Key, dateVal.Value, (byte)queryTag.Level));
            }
        }

        private static void AddDateTimeWithUtcRow(DicomDataset instance, List<InsertDateTimeExtendedQueryTagTableTypeV2Row> dateTimeRows, QueryTag queryTag)
        {
            DateTime? dateVal = null;
            DateTime? dateUtcVal = null;

            switch (queryTag.VR.Code)
            {
                case "DT":
                    Tuple<DateTime?, DateTime?> localAndUtcDateTimes = DateTimeReaders.TryGetValue(
                                                                            queryTag.VR,
                                                                            out Func<DicomDataset, DicomTag, DicomVR, Tuple<DateTime?, DateTime?>> readerDT) ? readerDT.Invoke(instance, queryTag.Tag, queryTag.VR) : null;
                    dateVal = localAndUtcDateTimes.Item1;
                    dateUtcVal = localAndUtcDateTimes.Item2;
                    break;
                case "DA":
                default:
                    dateVal = DateReaders.TryGetValue(
                                    queryTag.VR,
                                    out Func<DicomDataset, DicomTag, DicomVR, DateTime?> readerDA) ? readerDA.Invoke(instance, queryTag.Tag, queryTag.VR) : null;
                    break;
            }

            if (dateVal.HasValue)
            {
                dateTimeRows.Add(new InsertDateTimeExtendedQueryTagTableTypeV2Row(queryTag.ExtendedQueryTagStoreEntry.Key, dateVal.Value, dateUtcVal.HasValue ? dateUtcVal.Value : null, (byte)queryTag.Level));
            }
        }

        private static void AddDoubleRow(DicomDataset instance, List<InsertDoubleExtendedQueryTagTableTypeV1Row> doubleRows, QueryTag queryTag)
        {
            double? doubleVal = instance.GetSingleValueOrDefault<double>(queryTag.Tag, expectedVR: queryTag.VR);
            if (doubleVal.HasValue)
            {
                doubleRows.Add(new InsertDoubleExtendedQueryTagTableTypeV1Row(queryTag.ExtendedQueryTagStoreEntry.Key, doubleVal.Value, (byte)queryTag.Level));
            }
        }

        private static void AddLongRow(DicomDataset instance, List<InsertLongExtendedQueryTagTableTypeV1Row> longRows, QueryTag queryTag)
        {
            long? longVal = LongReaders.TryGetValue(
                             queryTag.VR,
                             out Func<DicomDataset, DicomTag, DicomVR, long?> reader) ? reader.Invoke(instance, queryTag.Tag, queryTag.VR) :
                             instance.GetSingleValueOrDefault<long>(queryTag.Tag, expectedVR: queryTag.VR);

            if (longVal.HasValue)
            {
                longRows.Add(new InsertLongExtendedQueryTagTableTypeV1Row(queryTag.ExtendedQueryTagStoreEntry.Key, longVal.Value, (byte)queryTag.Level));
            }
        }

        private static void AddStringRow(DicomDataset instance, List<InsertStringExtendedQueryTagTableTypeV1Row> stringRows, QueryTag queryTag)
        {
            string stringVal = instance.GetSingleValueOrDefault<string>(queryTag.Tag, expectedVR: queryTag.VR);
            if (stringVal != null)
            {
                stringRows.Add(new InsertStringExtendedQueryTagTableTypeV1Row(queryTag.ExtendedQueryTagStoreEntry.Key, stringVal, (byte)queryTag.Level));
            }
        }
    }
}
