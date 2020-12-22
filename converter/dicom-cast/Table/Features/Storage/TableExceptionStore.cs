// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.TableStorage.Configs;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage.Entities;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage
{
    public class TableExceptionStore : ITableStore
    {
        private readonly CloudTableClient _client;
        private readonly TableDataStoreConfiguration _configuration;

        public TableExceptionStore(CloudTableClient client, TableDataStoreConfiguration configuration)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            _client = client;
            _configuration = configuration;
        }

        public async Task StoreExceptionToTable(string studyId, string seriesId, string instanceId, Exception exceptionToStore, TableErrorType errorType, CancellationToken cancellationToken)
        {
            CloudTable table;
            TableEntity entity;

            if (errorType == TableErrorType.FhirError)
            {
                table = _client.GetTableReference(Constants.FhirTableName);
                entity = new FhirTransientEntity(studyId, seriesId, instanceId, exceptionToStore);
            }
            else
            {
                return;
            }

            await table.CreateIfNotExistsAsync(cancellationToken);
            TableOperation operation = TableOperation.InsertOrMerge(entity);
            try
            {
                TableResult result = await table.ExecuteAsync(operation, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new DataStoreException(ex);
            }
        }
    }
}
