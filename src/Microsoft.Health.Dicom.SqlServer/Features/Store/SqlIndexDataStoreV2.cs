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
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.CustomTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    internal class SqlIndexDataStoreV2 : SqlIndexDataStoreV1
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionFactoryWrapper;

        public SqlIndexDataStoreV2(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
            : base(sqlConnectionWrapperFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            _sqlConnectionFactoryWrapper = sqlConnectionWrapperFactory;
        }

        public override async Task<long> CreateInstanceIndexAsync(DicomDataset instance, IEnumerable<IndexTag> indexableDicomTags, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(indexableDicomTags, nameof(indexableDicomTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionFactoryWrapper.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                // Build parameter for custom tag.
                VLatest.AddInstanceTableValuedParameters parameters = AddInstanceTableValuedParametersBuilder.Build(
                    instance,
                    indexableDicomTags.Where(tag => tag.IsCustomTag));

                VLatest.AddInstance.PopulateCommand(
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
                (byte)IndexStatus.Creating,
                parameters);

                try
                {
                    return (long)(await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken));
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.Conflict:
                            {
                                if (ex.State == (byte)IndexStatus.Creating)
                                {
                                    throw new PendingInstanceException();
                                }

                                throw new InstanceAlreadyExistsException();
                            }
                    }

                    throw new DataStoreException(ex);
                }
            }
        }
    }
}
