// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Retrieve
{
    internal class SqlInstanceStore : IInstanceStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;

        public SqlInstanceStore(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
        }

        public Task<IEnumerable<VersionedInstanceIdentifier>> GetInstanceIdentifierAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            CancellationToken cancellationToken)
        {
            return GetInstanceIdentifierImp(studyInstanceUid, cancellationToken, seriesInstanceUid, sopInstanceUid);
        }

        public Task<IEnumerable<VersionedInstanceIdentifier>> GetInstanceIdentifiersInSeriesAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            CancellationToken cancellationToken)
        {
            return GetInstanceIdentifierImp(studyInstanceUid, cancellationToken, seriesInstanceUid);
        }

        public Task<IEnumerable<VersionedInstanceIdentifier>> GetInstanceIdentifiersInStudyAsync(
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            return GetInstanceIdentifierImp(studyInstanceUid, cancellationToken);
        }

        private async Task<IEnumerable<VersionedInstanceIdentifier>> GetInstanceIdentifierImp(
            string studyInstanceUid,
            CancellationToken cancellationToken,
            string seriesInstanceUid = null,
            string sopInstanceUid = null)
        {
            var results = new List<VersionedInstanceIdentifier>();

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapper())
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.GetInstance.PopulateCommand(
                    sqlCommandWrapper,
                    validStatus: (byte)IndexStatus.Created,
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (string rStudyInstanceUid, string rSeriesInstanceUid, string rSopInstanceUid, long watermark) = reader.ReadRow(
                           VLatest.Instance.StudyInstanceUid,
                           VLatest.Instance.SeriesInstanceUid,
                           VLatest.Instance.SopInstanceUid,
                           VLatest.Instance.Watermark);

                        results.Add(new VersionedInstanceIdentifier(
                                rStudyInstanceUid,
                                rSeriesInstanceUid,
                                rSopInstanceUid,
                                watermark));
                    }
                }
            }

            return results;
        }

        public async Task<string> GetETagForStudyAsync(
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            return await GetETagImp(cancellationToken, studyInstanceUid: studyInstanceUid);
        }

        public async Task<string> GetETagForSeriesAsync(
            string seriesInstanceUid,
            CancellationToken cancellationToken)
        {
            return await GetETagImp(cancellationToken, seriesInstanceUid: seriesInstanceUid);
        }

        public async Task<string> GetETagForInstanceAsync(
            string sopInstanceUid,
            CancellationToken cancellationToken)
        {
            return await GetETagImp(cancellationToken, sopInstanceUid: sopInstanceUid);
        }

        private async Task<string> GetETagImp(
            CancellationToken cancellationToken,
            string studyInstanceUid = null,
            string seriesInstanceUid = null,
            string sopInstanceUid = null)
        {
            string result = string.Empty;

            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapper())
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.GetETag.PopulateCommand(
                    sqlCommandWrapper,
                    validStatus: (byte)IndexStatus.Created,
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        result = reader.GetString(0);
                    }
                }
            }

            return result;
        }
    }
}
