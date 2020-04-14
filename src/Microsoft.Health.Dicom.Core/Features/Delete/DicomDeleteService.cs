// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Abstractions.Features.Transactions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Core.Features.Delete
{
    public class DicomDeleteService : IDicomDeleteService
    {
        private readonly IDicomIndexDataStore _dicomIndexDataStore;
        private readonly IDicomFileStore _dicomFileStore;
        private readonly ITransactionHandler _transactionHandler;

        public DicomDeleteService(IDicomIndexDataStore dicomIndexDataStore, IDicomFileStore dicomFileStore, ITransactionHandler transactionHandler)
        {
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));
            EnsureArg.IsNotNull(dicomFileStore, nameof(dicomFileStore));
            EnsureArg.IsNotNull(transactionHandler, nameof(transactionHandler));

            _dicomIndexDataStore = dicomIndexDataStore;
            _dicomFileStore = dicomFileStore;
            _transactionHandler = transactionHandler;
        }

        public async Task DeleteStudyAsync(string studyInstanceUid, CancellationToken cancellationToken)
        {
            await _dicomIndexDataStore.DeleteStudyIndexAsync(studyInstanceUid, cancellationToken);
        }

        public async Task DeleteSeriesAsync(string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken)
        {
            await _dicomIndexDataStore.DeleteSeriesIndexAsync(studyInstanceUid, seriesInstanceUid, cancellationToken);
        }

        public async Task DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken)
        {
            await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken);
        }

        public async Task CleanupDeletedInstancesAsync(CancellationToken cancellationToken = default)
        {
            using (var transactionScope = _transactionHandler.BeginTransaction())
            {
                IEnumerable<VersionedDicomInstanceIdentifier> deletedInstanceIdentifiers = await _dicomIndexDataStore.RetrieveDeletedInstancesAsync(cancellationToken);

                foreach (var deletedInstanceIdentifier in deletedInstanceIdentifiers)
                {
                    try
                    {
                        await _dicomFileStore.DeleteIfExistsAsync(deletedInstanceIdentifier, cancellationToken);
                        await _dicomIndexDataStore.DeleteDeletedInstanceAsync(
                            deletedInstanceIdentifier.StudyInstanceUid,
                            deletedInstanceIdentifier.SeriesInstanceUid,
                            deletedInstanceIdentifier.SopInstanceUid,
                            deletedInstanceIdentifier.Version,
                            cancellationToken);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }

                transactionScope.Complete();
            }
        }
    }
}
