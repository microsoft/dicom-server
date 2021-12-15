// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Dicom;
using EnsureThat;
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
        private readonly TableServiceClient _tableServiceClient;
        private readonly ILogger<TableExceptionStore> _logger;

        public TableExceptionStore(
            TableServiceClient tableServiceClient,
            ILogger<TableExceptionStore> logger)
        {
            EnsureArg.IsNotNull(tableServiceClient, nameof(tableServiceClient));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _tableServiceClient = tableServiceClient;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task WriteExceptionAsync(ChangeFeedEntry changeFeedEntry, Exception exceptionToStore, ErrorType errorType, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(changeFeedEntry, nameof(changeFeedEntry));

            string tableName = errorType switch
            {
                ErrorType.FhirError => Constants.FhirExceptionTableName,
                ErrorType.DicomError => Constants.DicomExceptionTableName,
                ErrorType.DicomValidationError => Constants.DicomValidationTableName,
                ErrorType.TransientFailure => Constants.TransientFailureTableName,
                _ => null,
            };

            if (tableName == null)
            {
                return;
            }

            DicomDataset dataset = changeFeedEntry.Metadata;
            string studyUid = dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            string seriesUid = dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
            string instanceUid = dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            long changeFeedSequence = changeFeedEntry.Sequence;

            var tableClient = _tableServiceClient.GetTableClient(tableName);
            var entity = new IntransientEntity(studyUid, seriesUid, instanceUid, changeFeedSequence, exceptionToStore);

            try
            {
                await tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
                _logger.LogInformation("Error when processing changefeed entry: {ChangeFeedSequence} for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID}. Stored into table: {Table} in table storage.", changeFeedSequence, studyUid, seriesUid, instanceUid, tableName);
            }
            catch
            {
                _logger.LogInformation("Error when processing changefeed entry: {ChangeFeedSequence} for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID}. Failed to store to table storage.", changeFeedSequence, studyUid, seriesUid, instanceUid);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task WriteRetryableExceptionAsync(ChangeFeedEntry changeFeedEntry, int retryNum, TimeSpan nextDelayTimeSpan, Exception exceptionToStore, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(changeFeedEntry, nameof(changeFeedEntry));

            DicomDataset dataset = changeFeedEntry.Metadata;
            string studyUid = dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            string seriesUid = dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
            string instanceUid = dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            long changeFeedSequence = changeFeedEntry.Sequence;

            var tableClient = _tableServiceClient.GetTableClient(Constants.TransientRetryTableName);
            var entity = new RetryableEntity(studyUid, seriesUid, instanceUid, changeFeedSequence, retryNum, exceptionToStore);

            try
            {
                await tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
                _logger.LogInformation("Retryable error when processing changefeed entry: {ChangeFeedSequence} for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID}. Tried {RetryNum} time(s). Waiting {Milliseconds} milliseconds . Stored into table: {Table} in table storage.", changeFeedSequence, studyUid, seriesUid, instanceUid, retryNum, nextDelayTimeSpan.TotalMilliseconds, Constants.TransientRetryTableName);
            }
            catch
            {
                _logger.LogInformation("Retryable error when processing changefeed entry: {ChangeFeedSequence} for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID}. Tried {RetryNum} time(s). Failed to store to table storage.", changeFeedSequence, studyUid, seriesUid, instanceUid, retryNum);
                throw;
            }
        }
    }
}
