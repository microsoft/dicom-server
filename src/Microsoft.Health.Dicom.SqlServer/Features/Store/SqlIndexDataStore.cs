// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Storage;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    internal class SqlIndexDataStore : IIndexDataStore
    {
        private readonly SqlIndexSchema _sqlServerIndexSchema;
        private readonly SqlConnectionWrapperFactory _sqlConnectionFactoryWrapper;
        private readonly SchemaInformation _schemaInformation;
        private IIndexDataStore _innerDataStore;

        public SqlIndexDataStore(
            SqlIndexSchema indexSchema,
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
            SchemaInformation schemaInformation)
        {
            EnsureArg.IsNotNull(indexSchema, nameof(indexSchema));
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
            _sqlServerIndexSchema = indexSchema;
            _sqlConnectionFactoryWrapper = sqlConnectionWrapperFactory;
            _schemaInformation = schemaInformation;
        }

        public async Task<long> CreateInstanceIndexAsync(DicomDataset dicomDataset, IEnumerable<IndexTag> indexTags, CancellationToken cancellationToken)
        {
            await InitInnerDataStoreAsync();
            return await _innerDataStore.CreateInstanceIndexAsync(dicomDataset, indexTags, cancellationToken);
        }

        public async Task DeleteDeletedInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
        {
            await InitInnerDataStoreAsync();
            await _innerDataStore.DeleteDeletedInstanceAsync(versionedInstanceIdentifier, cancellationToken);
        }

        public async Task DeleteInstanceIndexAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            await InitInnerDataStoreAsync();
            await _innerDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, cancellationToken);
        }

        public async Task DeleteSeriesIndexAsync(string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            await InitInnerDataStoreAsync();
            await _innerDataStore.DeleteSeriesIndexAsync(studyInstanceUid, seriesInstanceUid, cleanupAfter, cancellationToken);
        }

        public async Task DeleteStudyIndexAsync(string studyInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            await InitInnerDataStoreAsync();
            await _innerDataStore.DeleteStudyIndexAsync(studyInstanceUid, cleanupAfter, cancellationToken);
        }

        public async Task<int> IncrementDeletedInstanceRetryAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            await InitInnerDataStoreAsync();
            return await _innerDataStore.IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier, cleanupAfter, cancellationToken);
        }

        public async Task<IEnumerable<VersionedInstanceIdentifier>> RetrieveDeletedInstancesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken)
        {
            await InitInnerDataStoreAsync();
            return await _innerDataStore.RetrieveDeletedInstancesAsync(batchSize, maxRetries, cancellationToken);
        }

        public async Task UpdateInstanceIndexStatusAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, IndexStatus status, CancellationToken cancellationToken)
        {
            await InitInnerDataStoreAsync();
            await _innerDataStore.UpdateInstanceIndexStatusAsync(versionedInstanceIdentifier, status, cancellationToken);
        }

        private async Task InitInnerDataStoreAsync()
        {
            await _sqlServerIndexSchema.EnsureInitialized();

            if (_schemaInformation.Current == 1)
            {
                _innerDataStore = new SqlIndexDataStoreV1(_sqlConnectionFactoryWrapper);
            }
            else
            {
                _innerDataStore = new SqlIndexDataStoreV2(_sqlConnectionFactoryWrapper);
            }
        }
    }
}
