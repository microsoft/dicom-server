// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using EnsureThat;
using Microsoft.Extensions.Logging;

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
        public void StoreException(string studyUid, string seriesUid, string instanceUid, long changeFeedSequence, Exception exceptionToStore, ErrorType errorType, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(exceptionToStore, "Error when processsing changefeed entry: {ChangeFeedSequence} for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID}.", changeFeedSequence, studyUid, seriesUid, instanceUid);
        }
    }
}
