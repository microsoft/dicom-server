// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence;

/// <summary>
///  Tests for backfilling content length, which can be removed as a whole file when we no longer need it.
/// Storing of correct content length is tested in other test classes.
/// Can not be run in parallel as each inserts data and each queries ranges of these inserts for testing.
/// </summary>
[Collection("Content Length BackFill Collection")]
public class ContentLengthBackfillTests : IClassFixture<SqlDataStoreTestsFixture>
{
    private readonly IInstanceStore _instanceStore;
    private readonly IIndexDataStore _indexDataStore;
    private readonly SqlDataStoreTestsFixture _fixture;

    private static readonly long ExpectedFilePropertiesContentLength = 123;
    private static readonly FileProperties ZeroLengthFileProperties = new()
    {
        Path = "zeroLength.dcm",
        ETag = "E1230",
        ContentLength = 0
    };

    private static readonly string StudyInstanceUid = TestUidGenerator.Generate();

    public ContentLengthBackfillTests(SqlDataStoreTestsFixture fixture)
    {
        _fixture = EnsureArg.IsNotNull(fixture, nameof(fixture));
        _instanceStore = EnsureArg.IsNotNull(fixture?.InstanceStore, nameof(fixture.InstanceStore));
        _indexDataStore = EnsureArg.IsNotNull(fixture?.IndexDataStore, nameof(fixture.IndexDataStore));
    }


    [Fact]
    public async Task GivenInstanceWithZeroAsContentLength_WhenInBetweenInstancesWithLengthSet_ExpectWatermarkRangeReturnedExcludesTheBeforeAndAfterInstances()
    {
        // We will ask for batch size of 3.
        // We will insert 3 instances. The second instance will have a content length that is not a 0. We expect the watermark 
        // range to only include this instance and not the instance that came before or after.
        var instances = new List<VersionedInstanceIdentifier>
        {
            await CreateRandomInstanceAsync(),
            await CreateRandomInstanceAsync(fileProperties: ZeroLengthFileProperties), // This instance will have a content length of 0.
            await CreateRandomInstanceAsync(),
        };

        IReadOnlyList<WatermarkRange> batches = await _instanceStore.GetContentLengthBackFillInstanceBatches(batchSize: 3, batchCount: 2);

        Assert.Single(batches);
        var firstBatch = batches[0];

        Assert.Equal(new WatermarkRange(instances[1].Version, instances[1].Version), firstBatch);

        // cleanup so other tests are not affected
        await _indexDataStore.DeleteStudyIndexAsync(Partition.Default, StudyInstanceUid, DateTime.Now, CancellationToken.None);
    }


    [Fact]
    public async Task GivenInstancesWithZeroAsContentLength_WhenTheresAnInstanceWithLengthSetInBetween_ExpectWatermarkRangeReturnedRepresentsGapsAndBatchSize()
    {
        FileProperties zeroLengthFileProperties = new FileProperties { Path = "zeroLength.dcm", ETag = "E1230", ContentLength = 0 };

        // We will insert 5 instances. The second instance will have a content length that is set, but all others will
        // have a length of 0.

        var instances = new List<VersionedInstanceIdentifier>
        {
            await CreateRandomInstanceAsync(fileProperties: zeroLengthFileProperties),
            await CreateRandomInstanceAsync(), // This instance will have a content length set/ non zero.
            await CreateRandomInstanceAsync(fileProperties: zeroLengthFileProperties),
            await CreateRandomInstanceAsync(fileProperties: zeroLengthFileProperties),
            await CreateRandomInstanceAsync(fileProperties: zeroLengthFileProperties),
        };

        // When batch size is 4, expect the watermark range to be from first instance to the last instance, which
        // will include the second instance as it is just a range, but the range values will reflect that we don't 
        //  don't intend to batch update his second instance. This ensures the range size is consistent with what the
        // update query will be, ensuring we have a steady performance.
        IReadOnlyList<WatermarkRange> batchesFourSize = await _instanceStore.GetContentLengthBackFillInstanceBatches(batchSize: 4, batchCount: 2);

        Assert.Single(batchesFourSize);
        var firstBatchFourSize = batchesFourSize[0];

        Assert.Equal(new WatermarkRange(instances[0].Version, instances[4].Version), firstBatchFourSize);


        // When batch size is 3, expect first the watermark range to be from second instance but excluding it to the last instance.
        // The second batch will be just the first instance.
        IReadOnlyList<WatermarkRange> batches = await _instanceStore.GetContentLengthBackFillInstanceBatches(batchSize: 3, batchCount: 2);

        Assert.Equal(2, batches.Count);
        var firstBatch = batches[0];
        var secondBatch = batches[1];

        Assert.Equal(new WatermarkRange(instances[2].Version, instances[4].Version), firstBatch);
        Assert.Equal(new WatermarkRange(instances[0].Version, instances[0].Version), secondBatch);

        // cleanup so other tests are not affected
        await _indexDataStore.DeleteStudyIndexAsync(Partition.Default, StudyInstanceUid, DateTime.Now, CancellationToken.None);
    }


    [Fact]
    public async Task GivenWatermarkRangeIncludesInstancesNotNeedingContentLengthBackfill_WhenGettingInstancesToBackfill_ExpectInstancesNotNeedingBackfillNotReturned()
    {
        FileProperties zeroLengthFileProperties = new FileProperties { Path = "zeroLength.dcm", ETag = "E1230", ContentLength = 0 };

        // We will insert 5 instances. The second instance will have a content length that is set, but all others will
        // have a length of 0.

        var instances = new List<VersionedInstanceIdentifier>
        {
            await CreateRandomInstanceAsync(fileProperties: zeroLengthFileProperties),
            await CreateRandomInstanceAsync(), // This instance will have a content length set/ non zero.
            await CreateRandomInstanceAsync(fileProperties: zeroLengthFileProperties),
            await CreateRandomInstanceAsync(fileProperties: zeroLengthFileProperties),
            await CreateRandomInstanceAsync(fileProperties: zeroLengthFileProperties),
        };
        var nonZeroLengthInstance = instances[1];
        // When batch size is 4, expect the watermark range to be from first instance to the last instance, which
        // will include the second instance as it is just a range, but the range values will reflect that we don't 
        //  don't intend to batch update his second instance. This ensures the range size is consistent with what the
        // update query will be, ensuring we have a steady performance.
        IReadOnlyList<WatermarkRange> batches = await _instanceStore.GetContentLengthBackFillInstanceBatches(batchSize: 4, batchCount: 2);

        Assert.Single(batches);
        var firstBatch = batches[0];

        Assert.Equal(new WatermarkRange(instances[0].Version, instances[4].Version), firstBatch);

        var identitiers = await _instanceStore.GetContentLengthBackFillInstanceIdentifiersByWatermarkRangeAsync(firstBatch);
        Assert.Equal(4, identitiers.Count);
        instances.Remove(nonZeroLengthInstance);
        Assert.All(instances, i => Assert.Contains(i, identitiers));

        // cleanup so other tests are not affected
        await _indexDataStore.DeleteStudyIndexAsync(Partition.Default, StudyInstanceUid, DateTime.Now, CancellationToken.None);
    }


    [Fact]
    public async Task GivenFilePropertiesToUpdate_WhenUpdated_ExpectContentLengthUpdatedOnEachThatHadOriginalLengthOfZero()
    {
        FileProperties zeroLengthFileProperties = new FileProperties { Path = "zeroLength.dcm", ETag = "E1230", ContentLength = 0 };

        // We will insert 5 instances. The second instance will have a content length that is set, but all others will
        // have a length of 0.

        var instances = new List<VersionedInstanceIdentifier>
        {
            await CreateRandomInstanceAsync(fileProperties: zeroLengthFileProperties),
            await CreateRandomInstanceAsync(), // This instance will have a content length set/ non zero.
            await CreateRandomInstanceAsync(fileProperties: zeroLengthFileProperties),
            await CreateRandomInstanceAsync(fileProperties: zeroLengthFileProperties),
            await CreateRandomInstanceAsync(fileProperties: zeroLengthFileProperties),
        };
        var nonZeroLengthInstance = instances[1];

        // When batch size is 4, expect the watermark range to be from first instance to the last instance, which
        // will include the second instance as it is just a range, but the range values will reflect that we don't 
        //  don't intend to batch update his second instance. This ensures the range size is consistent with what the
        // update query will be, ensuring we have a steady performance.
        IReadOnlyList<WatermarkRange> batches = await _instanceStore.GetContentLengthBackFillInstanceBatches(batchSize: 4, batchCount: 2);

        Assert.Single(batches);

        var newFileProperties = new FileProperties()
        {
            ContentLength = 123456789,
            ETag = "newETag",
            Path = "newFilePath"
        };

        var identitiersToUpdate = await _instanceStore.GetContentLengthBackFillInstanceIdentifiersByWatermarkRangeAsync(batches[0]);
        Assert.Equal(4, identitiersToUpdate.Count);

        var filePropertiesByWatermarkToUpdate = new Dictionary<long, FileProperties>();

        foreach (var identifier in identitiersToUpdate)
        {
            filePropertiesByWatermarkToUpdate.TryAdd(identifier.Version, newFileProperties);
        }

        await _indexDataStore.UpdateFilePropertiesContentLengthAsync(filePropertiesByWatermarkToUpdate);
        foreach (var identifier in identitiersToUpdate)
        {
            IReadOnlyList<FileProperty> results = await _fixture.IndexDataStoreTestHelper.GetFilePropertiesAsync(identifier.Version);
            Assert.Single(results);
            var result = results[0];
            // assert that we have updated content length on each instance, but not other fields
            Assert.Equal(newFileProperties.ContentLength, result.ContentLength);
            Assert.NotEqual(newFileProperties.ETag, result.ETag);
            Assert.NotEqual(newFileProperties.Path, result.FilePath);
        }

        // assert that nonZeroLengthInstance was not updated at all
        IReadOnlyList<FileProperty> resultsNonZeroInstance = await _fixture.IndexDataStoreTestHelper.GetFilePropertiesAsync(nonZeroLengthInstance.Version);
        Assert.Single(resultsNonZeroInstance);
        var resultNonZeroInstance = resultsNonZeroInstance[0];
        // assert that we have updated content length on each instance, but not other fields
        Assert.NotEqual(newFileProperties.ContentLength, resultNonZeroInstance.ContentLength);
        Assert.Equal(ExpectedFilePropertiesContentLength, resultNonZeroInstance.ContentLength);
        Assert.NotEqual(newFileProperties.ETag, resultNonZeroInstance.ETag);
        Assert.NotEqual(newFileProperties.Path, resultNonZeroInstance.FilePath);

        // cleanup so other tests are not affected
        await _indexDataStore.DeleteStudyIndexAsync(Partition.Default, StudyInstanceUid, DateTime.Now, CancellationToken.None);
    }

    private static FileProperties CreateFileProperties(bool createFileProperty, long watermark)
    {
        if (createFileProperty)
        {
            return new FileProperties
            {
                Path = $"test/file_{watermark}.dcm",
                ETag = $"etag_{watermark}",
                ContentLength = ExpectedFilePropertiesContentLength,
            };
        }

        return null;
    }

    private async Task<VersionedInstanceIdentifier> CreateRandomInstanceAsync(Partition partition = null, FileProperties fileProperties = null)
    {
        DicomDataset dataset = Samples.CreateRandomInstanceDataset(StudyInstanceUid);
        partition ??= Partition.Default;

        long watermark = await _indexDataStore.BeginCreateInstanceIndexAsync(partition, dataset);

        fileProperties ??= CreateFileProperties(true, watermark);
        await _indexDataStore.EndCreateInstanceIndexAsync(partition.Key, dataset, watermark, fileProperties);

        return new VersionedInstanceIdentifier(
            dataset.GetString(DicomTag.StudyInstanceUID),
            dataset.GetString(DicomTag.SeriesInstanceUID),
            dataset.GetString(DicomTag.SOPInstanceUID),
            watermark,
            partition);
    }
}
