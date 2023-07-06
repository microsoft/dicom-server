// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence;

public interface IIndexDataStoreTestHelper
{
    Task<IReadOnlyList<StudyMetadata>> GetStudyMetadataAsync(string studyInstanceUid, int partitionKey = Partition.DefaultKey);

    Task<IReadOnlyList<SeriesMetadata>> GetSeriesMetadataAsync(string seriesInstanceUid, int partitionKey = Partition.DefaultKey);

    Task<IReadOnlyList<Instance>> GetInstancesAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, int partitionKey = Partition.DefaultKey);

    Task<IReadOnlyList<FileProperty>> GetFilePropertiesAsync(long watermark);

    Task<Instance> GetInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, long version);

    Task<IReadOnlyList<DeletedInstance>> GetDeletedInstanceEntriesAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, int partitionKey = Partition.DefaultKey);

    Task<IReadOnlyList<ChangeFeedRow>> GetChangeFeedRowsAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid);

    Task<IReadOnlyList<ChangeFeedRow>> GetUpdatedChangeFeedRowsAsync(int limit);

    Task ClearDeletedInstanceTableAsync();

    Task ClearIndexTablesAsync();
}
