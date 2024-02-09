// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag.Models;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.DeleteExtendedQueryTag;

public partial class DeleteExtendedQueryTagFunctionTests
{
    [Fact]
    public async Task GivenTagData_WhenGetExtendedQueryTagBatches_ArgumentsArePassed()
    {
        string vr = "VR";
        int tagKey = 1;
        int batchSize = 10;
        int batchCount = 5;
        IReadOnlyList<WatermarkRange> expected = new List<WatermarkRange> { new WatermarkRange(12345, 678910) };

        _extendedQueryTagStore
            .GetExtendedQueryTagBatches(batchSize, batchCount, vr, tagKey, default)
            .Returns(expected);

        IReadOnlyList<WatermarkRange> actual = await _deleteExtendedQueryTagFunction.GetExtendedQueryTagBatchesAsync(
            new BatchCreationArguments(tagKey, vr, batchSize, batchCount),
            NullLogger.Instance);

        Assert.Same(expected, actual);
        await _extendedQueryTagStore
            .Received(1)
            .GetExtendedQueryTagBatches(batchSize, batchCount, vr, tagKey, default);
    }

    [Fact]
    public async Task GivenTagData_WhenDeleteExtendedQueryTagDataByWatermarkRange_ShouldInvokeMethod()
    {
        string vr = "VR";
        int tagKey = 1;
        WatermarkRange range = new WatermarkRange(12345, 678910);

        await _deleteExtendedQueryTagFunction.DeleteExtendedQueryTagDataByWatermarkRangeAsync(
            new DeleteBatchArguments(tagKey, vr, range),
            NullLogger.Instance);

        await _extendedQueryTagStore
            .Received(1)
            .DeleteExtendedQueryTagDataByWatermarkRangeAsync(range.Start, range.End, vr, tagKey, default);
    }

    [Fact]
    public async Task GivenTagData_WhenDeleteExtendedQueryTagEntry_ShouldInvokeMethod()
    {
        int tagKey = 1;

        await _deleteExtendedQueryTagFunction.DeleteExtendedQueryTagEntry(
            new DeleteExtendedQueryTagArguments { TagKey = tagKey },
            NullLogger.Instance);

        await _extendedQueryTagStore
            .Received(1)
            .DeleteExtendedQueryTagEntryAsync(tagKey, default);
    }
}
