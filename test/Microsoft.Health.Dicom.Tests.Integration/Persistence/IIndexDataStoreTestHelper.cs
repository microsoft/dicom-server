// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public interface IIndexDataStoreTestHelper
    {
        Task<IReadOnlyList<StudyMetadata>> GetStudyMetadataAsync(string studyInstanceUid);

        Task<IReadOnlyList<SeriesMetadata>> GetSeriesMetadataAsync(string seriesInstanceUid);

        Task<IReadOnlyList<Instance>> GetInstancesAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid);

        Task<Instance> GetInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, long version);

        Task<IReadOnlyList<DeletedInstance>> GetDeletedInstanceEntriesAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid);

        Task<IReadOnlyList<ChangeFeedRow>> GetChangeFeedRowsAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid);

        Task ClearDeletedInstanceTable();

        internal Task<IReadOnlyList<ExtendedQueryTagDataRow>> GetExtendedQueryTagDataAsync(
          ExtendedQueryTagDataType dataType,
          int tagKey,
          long studyKey,
          long? seriesKey = null,
          long? instanceKey = null,
          CancellationToken cancellationToken = default);

        internal Task<IReadOnlyList<ExtendedQueryTagDataRow>> GetExtendedQueryTagDataForTagKeyAsync(ExtendedQueryTagDataType dataType, int tagKey, CancellationToken cancellationToken = default);
    }
}
