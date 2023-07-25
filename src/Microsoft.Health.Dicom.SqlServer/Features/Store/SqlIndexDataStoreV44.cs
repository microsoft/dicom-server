// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

/// <summary>
/// Sql IndexDataStore version 44
/// </summary>
internal class SqlIndexDataStoreV44 : SqlIndexDataStoreV43
{
    public SqlIndexDataStoreV44(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V44;

    public override async Task EndUpdateInstanceAsync(
        int partitionKey,
        string studyInstanceUid,
        DicomDataset dicomDataset,
        IReadOnlyList<WatermarkedFileProperties> watermarkedFilePropertiesList,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        List<FilePropertyTableTypeRow> filePropertiesRows = watermarkedFilePropertiesList.Select(watermarkedFileProperties
            => new FilePropertyTableTypeRow(watermarkedFileProperties.Watermark, watermarkedFileProperties.Path, watermarkedFileProperties.ETag)).ToList();

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.EndUpdateInstanceV44.PopulateCommand(
                sqlCommandWrapper,
                partitionKey,
                studyInstanceUid,
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.PatientID),
                dicomDataset.GetFirstValueOrDefault<string>(DicomTag.PatientName),
                dicomDataset.GetStringDateAsDate(DicomTag.PatientBirthDate),
                filePropertiesRows);

            try
            {
                await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                throw ex.Number switch
                {
                    SqlErrorCodes.NotFound => new StudyNotFoundException(),
                    _ => new DataStoreException(ex),
                };
            }
        }
    }
}
