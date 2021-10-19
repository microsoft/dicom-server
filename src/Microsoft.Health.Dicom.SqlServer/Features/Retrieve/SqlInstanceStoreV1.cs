// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Retrieve
{
    internal class SqlInstanceStoreV1 : ISqlInstanceStore
    {
        public SqlInstanceStoreV1(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));

            SqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
        }

        protected SqlConnectionWrapperFactory SqlConnectionWrapperFactory { get; }

        public virtual SchemaVersion Version => SchemaVersion.V1;

        public virtual Task<IEnumerable<VersionedInstanceIdentifier>> GetInstanceIdentifierAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            CancellationToken cancellationToken)
        {
            return GetInstanceIdentifierImp(studyInstanceUid, cancellationToken, seriesInstanceUid, sopInstanceUid);
        }

        public virtual Task<IReadOnlyList<VersionedInstanceIdentifier>> GetInstanceIdentifiersByWatermarkRangeAsync(
            WatermarkRange watermarkRange,
            IndexStatus indexStatus,
            CancellationToken cancellationToken = default)
        {
            throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
        }

        public virtual Task<IEnumerable<VersionedInstanceIdentifier>> GetInstanceIdentifiersInSeriesAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            CancellationToken cancellationToken)
        {
            return GetInstanceIdentifierImp(studyInstanceUid, cancellationToken, seriesInstanceUid);
        }

        public virtual Task<IEnumerable<VersionedInstanceIdentifier>> GetInstanceIdentifiersInStudyAsync(
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            return GetInstanceIdentifierImp(studyInstanceUid, cancellationToken);
        }

        public virtual Task<IReadOnlyList<WatermarkRange>> GetInstanceBatchesAsync(
            int batchSize,
            int batchCount,
            IndexStatus indexStatus,
            long? maxWatermark = null,
            CancellationToken cancellationToken = default)
        {
            throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
        }

        private async Task<IEnumerable<VersionedInstanceIdentifier>> GetInstanceIdentifierImp(
            string studyInstanceUid,
            CancellationToken cancellationToken,
            string seriesInstanceUid = null,
            string sopInstanceUid = null)
        {
            var results = new List<VersionedInstanceIdentifier>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
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
    }
}
