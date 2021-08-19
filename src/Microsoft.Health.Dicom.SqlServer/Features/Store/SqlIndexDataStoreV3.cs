// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Linq;
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
    /// Sql IndexDataStore version 3.
    /// </summary>
    internal class SqlIndexDataStoreV3 : SqlIndexDataStoreV2
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionFactoryWrapper;

        public SqlIndexDataStoreV3(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
            : base(sqlConnectionWrapperFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            _sqlConnectionFactoryWrapper = sqlConnectionWrapperFactory;
        }

        public override SchemaVersion Version => SchemaVersion.V3;

        public override async Task<long> CreateInstanceIndexAsync(DicomDataset instance, IEnumerable<QueryTag> queryTags, string partitionId = null, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                // Build parameter for extended query tag.
                VLatest.AddInstanceTableValuedParameters parameters = AddInstanceTableValuedParametersBuilder.Build(
                    instance,
                    queryTags.Where(tag => tag.IsExtendedQueryTag));

                VLatest.AddInstance.PopulateCommand(
                sqlCommandWrapper,
                partitionId,
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
                instance.GetSingleValueOrDefault<string>(DicomTag.ManufacturerModelName),
                (byte)IndexStatus.Creating,
                parameters);

                try
                {
                    return (long)(await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken));
                }
                catch (SqlException ex)
                {
                    if (ex.Number == SqlErrorCodes.Conflict)
                    {
                        if (ex.State == (byte)IndexStatus.Creating)
                        {
                            throw new PendingInstanceException();
                        }

                        throw new InstanceAlreadyExistsException();
                    }

                    throw new DataStoreException(ex);
                }
            }
        }
    }
}
