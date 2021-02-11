// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage.Entities;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage.Models.Entities;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage
{
    /// <inheritdoc/>
    public class TableExceptionStore : IExceptionStore
    {
        private readonly CloudTableClient _client;
        private readonly ILogger<TableExceptionStore> _logger;

        public TableExceptionStore(
            CloudTableClient client,
            ILogger<TableExceptionStore> logger)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _client = client;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task WriteExceptionAsync(ChangeFeedEntry changeFeedEntry, Exception exceptionToStore, ErrorType errorType, CancellationToken cancellationToken)
        {
            CloudTable table;
            TableEntity entity;
            string tableName;

            switch (errorType)
            {
                case ErrorType.FhirError:
                    tableName = Constants.FhirExceptionTableName;
                    break;
                case ErrorType.DicomError:
                    tableName = Constants.DicomExceptionTableName;
                    break;
                case ErrorType.DicomValidationError:
                    tableName = Constants.DicomValidationTableName;
                    break;
                case ErrorType.TransientFailure:
                    tableName = Constants.TransientFailureTableName;
                    break;
                default:
                    return;
            }

            DicomDataset dataset = changeFeedEntry.Metadata;
            string studyUid = dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            string seriesUid = dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
            string instanceUid = dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            long changeFeedSequence = changeFeedEntry.Sequence;

            table = _client.GetTableReference(tableName);
            entity = new IntransientEntity(studyUid, seriesUid, instanceUid, changeFeedSequence, exceptionToStore);

            TableOperation operation = TableOperation.InsertOrMerge(entity);

            try
            {
                await table.ExecuteAsync(operation, cancellationToken);
                _logger.LogInformation("Error when processsing changefeed entry: {ChangeFeedSequence} for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID}. Stored into table: {Table} in table storage.", changeFeedSequence, studyUid, seriesUid, instanceUid, tableName);
            }
            catch
            {
                _logger.LogInformation("Error when processsing changefeed entry: {ChangeFeedSequence} for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID}. Failed to store to table storage.", changeFeedSequence, studyUid, seriesUid, instanceUid);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task WriteRetryableExceptionAsync(ChangeFeedEntry changeFeedEntry, int retryNum, TimeSpan nextDelayTimeSpan, Exception exceptionToStore, CancellationToken cancellationToken)
        {
            string tableName = Constants.TransientRetryTableName;

            DicomDataset dataset = changeFeedEntry.Metadata;
            string studyUid = dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            string seriesUid = dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
            string instanceUid = dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            long changeFeedSequence = changeFeedEntry.Sequence;

            CloudTable table = _client.GetTableReference(tableName);
            TableEntity entity = new RetryableEntity(studyUid, seriesUid, instanceUid, changeFeedSequence, retryNum, exceptionToStore);

            TableOperation operation = TableOperation.InsertOrMerge(entity);

            try
            {
                await table.ExecuteAsync(operation, cancellationToken);
                _logger.LogInformation("Retryable error when processsing changefeed entry: {ChangeFeedSequence} for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID}. Tried {retryNum} time(s). Waiting {milliseconds} milliseconds . Stored into table: {Table} in table storage.", changeFeedSequence, studyUid, seriesUid, instanceUid, retryNum, nextDelayTimeSpan.TotalMilliseconds, tableName);
            }
            catch
            {
                _logger.LogInformation("Retryable error when processsing changefeed entry: {ChangeFeedSequence} for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID}. Tried {retryNum} time(s). Failed to store to table storage.", changeFeedSequence, studyUid, seriesUid, instanceUid, retryNum);
                throw;
            }
        }
    }
}
