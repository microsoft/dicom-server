// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;

namespace Microsoft.Health.DicomCast.Core.Features.TableStorage
{
    /// <summary>
    /// This implementation of exception store does not store the errors due to table storage not being enabled, instead just log.
    /// </summary>
    public class NullExceptionStore : IExceptionStore
    {
        private readonly ILogger<NullExceptionStore> _logger;

        public NullExceptionStore(
            ILogger<NullExceptionStore> logger)
        {
            EnsureArg.IsNotNull(logger);

            _logger = logger;
        }

        /// <inheritdoc/>
        public void StoreException(string studyUid, string seriesUid, string instanceUid, long changeFeedSequence, Exception exceptionToStore, TableErrorType errorType, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(exceptionToStore, "Error when processsing changefeed entry: {ChangeFeedSequence} for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID}.", changeFeedSequence, studyUid, seriesUid, instanceUid);
        }
    }
}
