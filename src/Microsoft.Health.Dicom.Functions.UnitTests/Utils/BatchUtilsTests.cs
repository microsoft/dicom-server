// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.Utils;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Utils;
public class BatchUtilsTests
{
    [Fact]
    public async Task GivenIdsNotDivisibleByThreadCount_WhenExecuteBatchAsync_ThenShouldSucceed()
    {
        VersionedInstanceIdentifier[] ids = RandomIds(5);
        ITaskCreator taskCreator = Substitute.For<ITaskCreator>();
        await BatchUtils.ExecuteBatchAsync(ids, 2, id => taskCreator.CreateAsync(id));

        // Assert
        foreach (var id in ids)
        {
            await taskCreator
                  .Received(1)
                  .CreateAsync(id);
        }
    }

    [Fact]
    public async Task GivenIdsDivisibleByThreadCount_WhenExecuteBatchAsync_ThenShouldSucceed()
    {
        VersionedInstanceIdentifier[] ids = RandomIds(6);
        ITaskCreator taskCreator = Substitute.For<ITaskCreator>();
        await BatchUtils.ExecuteBatchAsync(ids, 2, id => taskCreator.CreateAsync(id));

        // Assert
        foreach (var id in ids)
        {
            await taskCreator
                  .Received(1)
                  .CreateAsync(id);
        }
    }

    [Fact]
    public async Task GivenNoIds_WhenExecuteBatchAsync_ThenShouldNotCallTaskCreator()
    {
        VersionedInstanceIdentifier[] ids = RandomIds(0);
        ITaskCreator taskCreator = Substitute.For<ITaskCreator>();
        await BatchUtils.ExecuteBatchAsync(ids, 2, id => taskCreator.CreateAsync(id));

        // Assert
        await taskCreator
              .DidNotReceive()
              .CreateAsync(Arg.Any<VersionedInstanceIdentifier>());

    }

    [Fact]
    public void GivenAscendingBatches_WhenGetBatchRange_ThenShouldReturnExpected()
    {
        var range1 = new WatermarkRange(1, 3);
        var range2 = new WatermarkRange(4, 5);
        Assert.Equal(new WatermarkRange(1, 5), BatchUtils.GetBatchRange(new[] { range1, range2 }, ascending: true));
    }

    [Fact]
    public void GivenDescendingBatches_WhenGetBatchRange_ThenShouldReturnExpected()
    {
        var range1 = new WatermarkRange(4, 5);
        var range2 = new WatermarkRange(1, 3);
        Assert.Equal(new WatermarkRange(1, 5), BatchUtils.GetBatchRange(new[] { range1, range2 }, ascending: false));
    }

    private static VersionedInstanceIdentifier[] RandomIds(int count)
    {
        var ids = new VersionedInstanceIdentifier[count];
        for (int i = 0; i < count; i++)
        {
            ids[i] = new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), i);
        }
        return ids;
    }

    public interface ITaskCreator
    {
        Task CreateAsync(VersionedInstanceIdentifier id);
    }
}
