// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.DicomCast.Core.Features.ExceptionStorage
{
    /// <summary>
    /// Implementation of exception store that logs the errors but does not persist it in storage.
    /// </summary>
    public class LogExceptionStore : IExceptionStore
    {
        private readonly ILogger<LogExceptionStore> _logger;

        public LogExceptionStore(
            ILogger<LogExceptionStore> logger)
        {
            EnsureArg.IsNotNull(logger);

            _logger = logger;
        }

        /// <inheritdoc/>
        public Task WriteExceptionAsync(ChangeFeedEntry changeFeedEntry, Exception exceptionToStore, ErrorType errorType, CancellationToken cancellationToken = default)
        {
            DicomDataset dataset = changeFeedEntry.Metadata;
            string studyUid = dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            string seriesUid = dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
            string instanceUid = dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            long changeFeedSequence = changeFeedEntry.Sequence;

            _logger.LogInformation("Error when processsing changefeed entry: {ChangeFeedSequence} for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID}.", changeFeedSequence, studyUid, seriesUid, instanceUid);
            return Task.CompletedTask;
        }

        public Task WriteRetryableExceptionAsync(ChangeFeedEntry changeFeedEntry, int retryNum, Exception exceptionToStore, CancellationToken cancellationToken = default)
        {
            DicomDataset dataset = changeFeedEntry.Metadata;
            string studyUid = dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            string seriesUid = dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
            string instanceUid = dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            long changeFeedSequence = changeFeedEntry.Sequence;

            _logger.LogInformation("Retryable error when processsing changefeed entry: {ChangeFeedSequence} for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID}. Trued {retryNum} time(s).", changeFeedSequence, studyUid, seriesUid, instanceUid, retryNum);
            return Task.CompletedTask;
        }
    }
}
