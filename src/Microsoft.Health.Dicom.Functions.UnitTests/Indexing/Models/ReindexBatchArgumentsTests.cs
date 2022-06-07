// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
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

        Assert.Throws<ArgumentNullException>(() => new ReindexBatchArguments(null, range));
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

        var actual = new ReindexBatchArguments(queryTags, range);
        Assert.Same(queryTags, actual.QueryTags);
        Assert.Equal(range, actual.WatermarkRange);
    }
}
