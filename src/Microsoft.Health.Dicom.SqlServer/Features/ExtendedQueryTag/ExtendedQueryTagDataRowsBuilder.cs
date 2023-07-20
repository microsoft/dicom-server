// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;

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

        var stringRows = new HashSet<InsertStringExtendedQueryTagTableTypeV1Row>();
        var longRows = new HashSet<InsertLongExtendedQueryTagTableTypeV1Row>();
        var doubleRows = new HashSet<InsertDoubleExtendedQueryTagTableTypeV1Row>();
        var dateTimeRows = new HashSet<InsertDateTimeExtendedQueryTagTableTypeV1Row>();
        var dateTimeWithUtcRows = new HashSet<InsertDateTimeExtendedQueryTagTableTypeV2Row>();
        var personNameRows = new HashSet<InsertPersonNameExtendedQueryTagTableTypeV1Row>();

        foreach (QueryTag queryTag in queryTags.Where(x => x.IsExtendedQueryTag || x.WorkitemQueryTagStoreEntry != null))
        {
            if (queryTag.VR == DicomVR.SQ)
            {
                var dicomDatasets = instance.GetSequencePathValues(queryTag.WorkitemQueryTagStoreEntry.PathTags);
                foreach (var dataset in dicomDatasets)
                {
                    foreach (var dicomItem in dataset)
                    {
                        ExtendedQueryTagDataType dataType = ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[dicomItem.Tag.GetDefaultVR().Code];
                        AddRows(dataset, dataType, new QueryTag(dicomItem.Tag), GetKeyFromQueryTag(queryTag));
                    }
                }
            }
            else
            {
                ExtendedQueryTagDataType dataType = ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[queryTag.VR.Code];
                AddRows(instance, dataType, queryTag, GetKeyFromQueryTag(queryTag));
            }
        }

        void AddRows(DicomDataset instance, ExtendedQueryTagDataType dataType, QueryTag queryTag, int tagKey)
        {
            // Create row
            switch (dataType)
            {
                case ExtendedQueryTagDataType.StringData:
                    AddStringRow(instance, stringRows, queryTag, tagKey);
                    break;
                case ExtendedQueryTagDataType.LongData:
                    AddLongRow(instance, longRows, queryTag, tagKey);
                    break;
                case ExtendedQueryTagDataType.DoubleData:
                    AddDoubleRow(instance, doubleRows, queryTag, tagKey);
                    break;
                case ExtendedQueryTagDataType.DateTimeData:
                    AddDateTimeWithUtcRow(instance, dateTimeWithUtcRows, queryTag, tagKey);
                    break;
                case ExtendedQueryTagDataType.PersonNameData:
                    AddPersonNameRow(instance, personNameRows, queryTag, tagKey);
                    break;
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

    private static void AddPersonNameRow(DicomDataset instance, HashSet<InsertPersonNameExtendedQueryTagTableTypeV1Row> personNamRows, QueryTag queryTag, int tagKey)
    {
        string personNameVal = instance.GetFirstValueOrDefault<string>(queryTag.Tag, expectedVR: queryTag.VR);
        if (personNameVal != null)
        {
            personNamRows.Add(new InsertPersonNameExtendedQueryTagTableTypeV1Row(tagKey, personNameVal, (byte)queryTag.Level));
        }
    }

    private static void AddDateTimeRow(DicomDataset instance, HashSet<InsertDateTimeExtendedQueryTagTableTypeV1Row> dateTimeRows, QueryTag queryTag, int tagKey)
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
            dateTimeRows.Add(new InsertDateTimeExtendedQueryTagTableTypeV1Row(tagKey, dateVal.Value, (byte)queryTag.Level));
        }
    }

    private static void AddDateTimeWithUtcRow(DicomDataset instance, HashSet<InsertDateTimeExtendedQueryTagTableTypeV2Row> dateTimeRows, QueryTag queryTag, int tagKey)
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
            dateTimeRows.Add(new InsertDateTimeExtendedQueryTagTableTypeV2Row(tagKey, dateVal.Value, dateUtcVal.HasValue ? dateUtcVal.Value : null, (byte)queryTag.Level));
        }
    }

    private static void AddDoubleRow(DicomDataset instance, HashSet<InsertDoubleExtendedQueryTagTableTypeV1Row> doubleRows, QueryTag queryTag, int tagKey)
    {
        double? doubleVal = instance.GetFirstValueOrDefault<double>(queryTag.Tag, expectedVR: queryTag.VR);
        if (doubleVal.HasValue)
        {
            doubleRows.Add(new InsertDoubleExtendedQueryTagTableTypeV1Row(tagKey, doubleVal.Value, (byte)queryTag.Level));
        }
    }

    private static void AddLongRow(DicomDataset instance, HashSet<InsertLongExtendedQueryTagTableTypeV1Row> longRows, QueryTag queryTag, int tagKey)
    {
        long? longVal = LongReaders.TryGetValue(queryTag.VR, out Func<DicomDataset, DicomTag, DicomVR, long?> reader)
            ? reader.Invoke(instance, queryTag.Tag, queryTag.VR)
            : instance.GetFirstValueOrDefault<long>(queryTag.Tag, queryTag.VR);

        if (longVal.HasValue)
        {
            longRows.Add(new InsertLongExtendedQueryTagTableTypeV1Row(tagKey, longVal.Value, (byte)queryTag.Level));
        }
    }

    private static void AddStringRow(DicomDataset instance, HashSet<InsertStringExtendedQueryTagTableTypeV1Row> stringRows, QueryTag queryTag, int tagKey)
    {
        string stringVal = instance.GetFirstValueOrDefault<string>(queryTag.Tag, expectedVR: queryTag.VR);
        if (stringVal != null)
        {
            stringRows.Add(new InsertStringExtendedQueryTagTableTypeV1Row(tagKey, stringVal, (byte)queryTag.Level));
        }
    }

    private static int GetKeyFromQueryTag(QueryTag queryTag)
    {
        return queryTag.WorkitemQueryTagStoreEntry != null ? queryTag.WorkitemQueryTagStoreEntry.Key : queryTag.ExtendedQueryTagStoreEntry.Key;
    }
}
