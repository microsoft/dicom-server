// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Features.ExtendedQueryTag
{
    public static class ExtendedQueryTagDataRowsBuilderTests
    {
        [Fact]
        internal static void GivenValidData_AndMatchingExtendedQueryTag_RowsAreBuilt()
        {
            var dataset = new DicomDataset(new DicomShortString(DicomTag.CodeValue, "FOO"));
            var queryTags = new List<QueryTag> { new QueryTag(new ExtendedQueryTagStoreEntry(1, "00080100", "SH", "", QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0)) };
            var rows = new ExtendedQueryTagDataRows();
            rows.StringRows = new HashSet<InsertStringExtendedQueryTagTableTypeV1Row> { new(1, "FOO", 0) };

            var result = ExtendedQueryTagDataRowsBuilder.Build(dataset, queryTags, (SchemaVersion)SchemaVersionConstants.Max);

            Assert.Equal(rows.StringRows, result.StringRows);
        }

        [Fact]
        internal static void GivenValidData_AndNonMatchingExtendedQueryTag_NoRowsAreBuilt()
        {
            var dataset = new DicomDataset(new DicomShortString(DicomTag.SnoutID, "FOO"));
            var queryTags = new List<QueryTag> { new QueryTag(new ExtendedQueryTagStoreEntry(1, "00080100", "SH", "", QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0)) };
            var rows = new ExtendedQueryTagDataRows();
            rows.StringRows = new HashSet<InsertStringExtendedQueryTagTableTypeV1Row> { new(1, "FOO", 0) };

            var result = ExtendedQueryTagDataRowsBuilder.Build(dataset, queryTags, (SchemaVersion)SchemaVersionConstants.Max);

            Assert.Empty(result.StringRows);
        }

        [Fact]
        internal static void GivenValidData_AndMatchingWorkitemQueryTag_RowsAreBuilt()
        {
            var dataset = new DicomDataset(new DicomShortString(DicomTag.CodeValue, "FOO"));
            var queryTags = new List<QueryTag> { new QueryTag(new WorkitemQueryTagStoreEntry(1, "00080100", "SH")) };
            var rows = new ExtendedQueryTagDataRows();
            rows.StringRows = new HashSet<InsertStringExtendedQueryTagTableTypeV1Row> { new(1, "FOO", 0) };

            var result = ExtendedQueryTagDataRowsBuilder.Build(dataset, queryTags, (SchemaVersion)SchemaVersionConstants.Max);

            Assert.Equal(rows.StringRows, result.StringRows);
        }

        [Fact]
        internal static void GivenValidData_AndMatchingMultilevelWorkitemQueryTag_RowsAreBuilt()
        {
            var dataset = new DicomDataset(
                new DicomSequence(
                    DicomTag.ScheduledStationNameCodeSequence,
                    new DicomDataset[]
                    {
                        new DicomDataset(
                            new DicomShortString(
                                DicomTag.CodeValue,
                                "FOO"))}));

            var entry = new WorkitemQueryTagStoreEntry(1, "00404025.00080100", "SQ");
            entry.PathTags = new List<DicomTag> { DicomTag.ScheduledStationNameCodeSequence, DicomTag.CodeValue }.AsReadOnly();
            var queryTags = new List<QueryTag> { new(entry) };

            var rows = new ExtendedQueryTagDataRows();
            rows.StringRows = new HashSet<InsertStringExtendedQueryTagTableTypeV1Row> { new(1, "FOO", 0) };

            var result = ExtendedQueryTagDataRowsBuilder.Build(dataset, queryTags, (SchemaVersion)SchemaVersionConstants.Max);

            Assert.Equal(rows.StringRows, result.StringRows);
        }
    }
}
