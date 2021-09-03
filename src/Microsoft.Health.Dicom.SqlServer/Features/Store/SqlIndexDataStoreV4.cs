// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    /// <summary>
    /// Sql IndexDataStore version 4.
    /// </summary>
    internal class SqlIndexDataStoreV4 : SqlIndexDataStoreV3
    {
        public SqlIndexDataStoreV4(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
            : base(sqlConnectionWrapperFactory)
        {
        }

        public override SchemaVersion Version => SchemaVersion.V4;

        public override async Task<long> BeginCreateInstanceIndexAsync(DicomDataset instance, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.BeginAddInstance.PopulateCommand(
                    sqlCommandWrapper,
                    instance.GetString(DicomTag.StudyInstanceUID),
                    instance.GetString(DicomTag.SeriesInstanceUID),
                    instance.GetString(DicomTag.SOPInstanceUID),
                    instance.GetSingleValueOrDefault<string>(DicomTag.PatientID),
                    instance.GetSingleValueOrDefault<string>(DicomTag.PatientName),
                    instance.GetSingleValueOrDefault<string>(DicomTag.ReferringPhysicianName),
                    instance.GetStringDateAsDate(DicomTag.StudyDate),
                    instance.GetSingleValueOrDefault<string>(DicomTag.StudyDescription),
                    instance.GetSingleValueOrDefault<string>(DicomTag.AccessionNumber),
                    instance.GetSingleValueOrDefault<string>(DicomTag.Modality),
                    instance.GetStringDateAsDate(DicomTag.PerformedProcedureStepStartDate),
                    instance.GetStringDateAsDate(DicomTag.PatientBirthDate),
                    instance.GetSingleValueOrDefault<string>(DicomTag.ManufacturerModelName));

                try
                {
                    return (long)(await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken));
                }
                catch (SqlException ex)
                {
                    throw ex.Number switch
                    {
                        SqlErrorCodes.Conflict => ex.State switch
                        {
                            (byte)IndexStatus.Creating => new PendingInstanceException(),
                            (byte)IndexStatus.Created => new InstanceAlreadyExistsException(),
                            _ => new ExtendedQueryTagVersionMismatchException(),
                        },
                        _ => new DataStoreException(ex),
                    };
                }
            }
        }

        public override async Task EndCreateInstanceIndexAsync(
            DicomDataset instance,
            long watermark,
            IEnumerable<QueryTag> queryTags,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            // Use maxTagVersion to track tag addition -- if new tag is added, max tag version increases.
            var rows = ExtendedQueryTagDataRowsBuilder.Build(instance, queryTags);
            VLatest.EndAddInstanceTableValuedParameters parameters = new VLatest.EndAddInstanceTableValuedParameters(
                rows.StringRows,
                rows.LongRows,
                rows.DoubleRows,
                rows.DateTimeRows,
                rows.PersonNameRows);

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.EndAddInstance.PopulateCommand(
                    sqlCommandWrapper,
                    instance.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                    instance.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                    instance.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
                    watermark,
                    UlongToRowVersion(rows.MaxVersion),
                    parameters);

                try
                {
                    await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    throw ex.Number switch
                    {
                        SqlErrorCodes.Conflict => new ExtendedQueryTagVersionMismatchException(),
                        SqlErrorCodes.NotFound => new InstanceNotFoundException(),
                        _ => new DataStoreException(ex),
                    };
                }
            }
        }

        public override async Task ReindexInstanceAsync(DicomDataset instance, long watermark, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                var rows = ExtendedQueryTagDataRowsBuilder.Build(instance, queryTags);
                VLatest.IndexInstanceTableValuedParameters parameters = new VLatest.IndexInstanceTableValuedParameters(
                    rows.StringRows,
                    rows.LongRows,
                    rows.DoubleRows,
                    rows.DateTimeRows,
                    rows.PersonNameRows);

                VLatest.IndexInstance.PopulateCommand(sqlCommandWrapper, watermark, parameters);

                try
                {
                    await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    throw ex.Number switch
                    {
                        SqlErrorCodes.NotFound => new InstanceNotFoundException(),
                        SqlErrorCodes.Conflict => new PendingInstanceException(),
                        _ => new DataStoreException(ex),
                    };

                }
            }
        }

        private static byte[] UlongToRowVersion(ulong? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            byte[] result = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(result, value.Value);
            return result;
        }
    }
}
