// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Retrieve
{
    internal class DicomSqlInstanceStore : IDicomInstanceStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;

        public DicomSqlInstanceStore(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
        }

        public Task<IEnumerable<DicomInstanceIdentifier>> GetInstanceIdentifierAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            CancellationToken cancellationToken)
        {
            return GetInstanceIdentifierImp(studyInstanceUid, cancellationToken, seriesInstanceUid, sopInstanceUid);
        }

        public Task<IEnumerable<DicomInstanceIdentifier>> GetInstanceIdentifiersInSeriesAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            CancellationToken cancellationToken)
        {
            return GetInstanceIdentifierImp(studyInstanceUid, cancellationToken, seriesInstanceUid);
        }

        public Task<IEnumerable<DicomInstanceIdentifier>> GetInstanceIdentifiersInStudyAsync(
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            return GetInstanceIdentifierImp(studyInstanceUid, cancellationToken);
        }

        private async Task<IEnumerable<DicomInstanceIdentifier>> GetInstanceIdentifierImp(
            string studyInstanceUid,
            CancellationToken cancellationToken,
            string seriesInstanceUid = null,
            string sopInstanceUid = null)
        {
            var results = new List<DicomInstanceIdentifier>();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapper())
            using (SqlCommand sqlCommand = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.GetInstance.PopulateCommand(
                    sqlCommand,
                    invalidStatus: DicomIndexStatus.Creating,
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid);

                using (var reader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (string rStudyInstanceUid, string rSeriesInstanceUid, string rSopInstanceUid, long watermark) = reader.ReadRow(
                           VLatest.Instance.StudyInstanceUid,
                           VLatest.Instance.SeriesInstanceUid,
                           VLatest.Instance.SopInstanceUid,
                           VLatest.Instance.Watermark);

                        results.Add(new VersionedDicomInstanceIdentifier(
                                rStudyInstanceUid,
                                rSeriesInstanceUid,
                                rSopInstanceUid,
                                watermark));
                    }
                }
            }

            return results;
        }
    }
}
