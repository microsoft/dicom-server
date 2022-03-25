// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.Indexing;
using Microsoft.Health.Dicom.Functions.Indexing.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Indexing.Models;

public class ReindexBatchArgumentsTests
{
    [Fact]
    public void GivenBadValues_WhenContructing_ThenThrowExceptions()
    {
        var queryTags = new List<ExtendedQueryTagStoreEntry>
        {
            new ExtendedQueryTagStoreEntry(1, "01", "DT", "foo", QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
            new ExtendedQueryTagStoreEntry(2, "02", "DT", "bar", QueryTagLevel.Study, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
        };
        var range = new WatermarkRange(5, 10);
        const int threadCount = 7;

        Assert.Throws<ArgumentNullException>(() => new ReindexBatchArguments(null, range, threadCount));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReindexBatchArguments(queryTags, range, -threadCount));
    }

    [Fact]
    public void GivenValues_WhenConstructing_ThenAssignProperties()
    {
        var queryTags = new List<ExtendedQueryTagStoreEntry>
        {
            new ExtendedQueryTagStoreEntry(1, "01", "DT", "foo", QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
            new ExtendedQueryTagStoreEntry(2, "02", "DT", "bar", QueryTagLevel.Study, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
        };
        var range = new WatermarkRange(5, 10);
        const int threadCount = 7;

        var actual = new ReindexBatchArguments(queryTags, range, threadCount);
        Assert.Same(queryTags, actual.QueryTags);
        Assert.Equal(range, actual.WatermarkRange);
        Assert.Equal(threadCount, actual.ThreadCount);
    }

    [Fact]
    public void GivenOptions_WhenCreatingFromOptions_ThenAssignProperties()
    {
        var queryTags = new List<ExtendedQueryTagStoreEntry>
        {
            new ExtendedQueryTagStoreEntry(1, "01", "DT", "foo", QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
            new ExtendedQueryTagStoreEntry(2, "02", "DT", "bar", QueryTagLevel.Study, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
        };
        var range = new WatermarkRange(5, 10);
        const int threadCount = 7;

        var actual = ReindexBatchArguments.FromOptions(
            queryTags,
            range,
            new QueryTagIndexingOptions { BatchThreadCount = threadCount });

        Assert.Same(queryTags, actual.QueryTags);
        Assert.Equal(range, actual.WatermarkRange);
        Assert.Equal(threadCount, actual.ThreadCount);
    }
}
