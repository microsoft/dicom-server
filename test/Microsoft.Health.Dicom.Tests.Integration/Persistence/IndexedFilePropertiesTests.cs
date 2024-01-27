// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence;

/// <summary>
/// Can not be run in parallel as each inserts data and each queries all of these inserts for testing.
/// </summary>
[Collection("Indexed File Properties Collection")]
public class IndexedFilePropertiesTests : IClassFixture<SqlDataStoreTestsFixture>
{
    private readonly IIndexDataStore _indexDataStore;
    private readonly SqlDataStoreTestsFixture _fixture;

    private static readonly string StudyInstanceUid = TestUidGenerator.Generate();
    private readonly FileProperties _fileProperties = new() { Path = "path.dcm", ETag = "E1230", ContentLength = 10 };

    public IndexedFilePropertiesTests(SqlDataStoreTestsFixture fixture)
    {
        _fixture = EnsureArg.IsNotNull(fixture, nameof(fixture));
        _indexDataStore = EnsureArg.IsNotNull(fixture?.IndexDataStore, nameof(fixture.IndexDataStore));
    }

    [Fact]
    public async Task GivenNoFilePropertiesIndexed_WhenGetIndexedFilePropertiesAsync_ExpectCorrectTotalsRetrieved()
    {
        IndexedFileProperties indexedFileProperties = await _indexDataStore.GetIndexedFilePropertiesAsync();

        Assert.Equal(0, indexedFileProperties.TotalSum);
        Assert.Equal(0, indexedFileProperties.TotalIndexed);
    }

    [Fact]
    public async Task GivenFilePropertiesIndexed_WhenGetIndexedFilePropertiesAsync_ExpectCorrectTotalsRetrieved()
    {
        var properties = new List<FileProperties>
        {
            await CreateRandomInstanceAsync(fileProperties: _fileProperties),
            await CreateRandomInstanceAsync(fileProperties: _fileProperties),
            await CreateRandomInstanceAsync(fileProperties: _fileProperties),
            await CreateRandomInstanceAsync(fileProperties: _fileProperties),
        };

        IndexedFileProperties indexedFileProperties = await _indexDataStore.GetIndexedFilePropertiesAsync();

        Assert.Equal(properties.Sum(x => x.ContentLength), indexedFileProperties.TotalSum);
        Assert.Equal(properties.Count, indexedFileProperties.TotalIndexed);
    }

    [Fact]
    public async Task GivenFilePropertiesIndexedWithZeroes_WhenGetIndexedFilePropertiesAsync_ExpectCorrectTotalsRetrieved()
    {
        FileProperties zeroLengthFileProperties = new FileProperties { Path = "zeroLength.dcm", ETag = "E1230", ContentLength = 0 };

        var properties = new List<FileProperties>
        {
            await CreateRandomInstanceAsync(fileProperties: _fileProperties),
            await CreateRandomInstanceAsync(fileProperties: zeroLengthFileProperties),
            await CreateRandomInstanceAsync(fileProperties: _fileProperties),
        };

        IndexedFileProperties indexedFileProperties = await _indexDataStore.GetIndexedFilePropertiesAsync();

        Assert.Equal(properties.Sum(x => x.ContentLength), indexedFileProperties.TotalSum);
        Assert.Equal(properties.Count, indexedFileProperties.TotalIndexed);
    }

    private async Task<FileProperties> CreateRandomInstanceAsync(FileProperties fileProperties, Partition partition = null)
    {
        DicomDataset dataset = Samples.CreateRandomInstanceDataset(StudyInstanceUid);
        partition ??= Partition.Default;

        long watermark = await _indexDataStore.BeginCreateInstanceIndexAsync(partition, dataset);

        await _indexDataStore.EndCreateInstanceIndexAsync(partition.Key, dataset, watermark, fileProperties);

        return fileProperties;
    }
}