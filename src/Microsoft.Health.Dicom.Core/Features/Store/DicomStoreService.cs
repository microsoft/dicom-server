// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to process and store the DICOM instance entries.
    /// </summary>
    public class DicomStoreService : IDicomStoreService
    {
        private readonly IDicomStorePersistenceOrchestrator _dicomStoreOrchestrator;
        private readonly Func<DicomStoreResponseBuilder> _dicomStoreResponseBuilderFactory;
        private readonly ILogger<DicomStoreService> _logger;

        public DicomStoreService(
            IDicomStorePersistenceOrchestrator dicomStoreOrchestrator,
            Func<DicomStoreResponseBuilder> dicomStoreResponseBuilderFactory,
            ILogger<DicomStoreService> logger)
        {
            EnsureArg.IsNotNull(dicomStoreOrchestrator, nameof(dicomStoreOrchestrator));
            EnsureArg.IsNotNull(dicomStoreResponseBuilderFactory, nameof(dicomStoreResponseBuilderFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomStoreOrchestrator = dicomStoreOrchestrator;
            _dicomStoreResponseBuilderFactory = dicomStoreResponseBuilderFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<DicomStoreResponse> ProcessDicomInstanceEntriesAsync(
            string studyInstanceUid,
            IReadOnlyCollection<IDicomInstanceEntry> dicomInstanceEntries,
            CancellationToken cancellationToken)
        {
            var responseBuilder = _dicomStoreResponseBuilderFactory();

            foreach (IDicomInstanceEntry dicomInstanceEntry in dicomInstanceEntries)
            {
                DicomDataset dicomDataset;

                try
                {
                    dicomDataset = await dicomInstanceEntry.GetDicomDatasetAsync(cancellationToken);
                }
                catch (InvalidDicomInstanceException)
                {
                    _logger.LogDebug("The DICOM instance could not be parsed.");

                    responseBuilder.AddFailure();

                    continue;
                }

                if (studyInstanceUid != null)
                {
                    string specifiedStudyInstanceUid = dicomDataset.GetSingleValueOrDefault<string>(DicomTag.StudyInstanceUID);

                    // The StudyInstanceUID was supplied, validate to make sure all supplied instances belongs to this study.
                    if (!studyInstanceUid.Equals(specifiedStudyInstanceUid, StringComparison.InvariantCultureIgnoreCase))
                    {
                        _logger.LogDebug(
                            "The DICOM instance does not belong to the same study as specified. Required StudyInstanceUID: {RequiredStudyInstanceUID}. SpecifiedStudyInstanceUID: {SpecifiedStudyInstanceUID}.",
                            studyInstanceUid,
                            specifiedStudyInstanceUid);

                        responseBuilder.AddFailure(dicomDataset, DicomStoreFailureCodes.MismatchStudyInstanceUidFailureCode);

                        continue;
                    }
                }

                try
                {
                    await _dicomStoreOrchestrator.PersistDicomInstanceEntryAsync(dicomInstanceEntry, cancellationToken);

                    responseBuilder.AddSuccess(dicomDataset);
                }
                catch (DicomDataStoreException ex) when (ex.StatusCode == (int)HttpStatusCode.Conflict)
                {
                    _logger.LogDebug("The specified DICOM instance already exists: '{DicomInstanceIdentifier}'.", dicomDataset.ToDicomInstanceIdentifier());

                    responseBuilder.AddFailure(dicomDataset, DicomStoreFailureCodes.SopInstanceAlreadyExistsFailureCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to store the DICOM instance.");

                    responseBuilder.AddFailure(dicomDataset);
                }
            }

            return responseBuilder.BuildResponse(studyInstanceUid);
        }
    }
}
