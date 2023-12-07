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
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

/// <summary>
/// Sql IndexDataStore version 52
/// </summary>
internal class SqlIndexDataStoreV52 : SqlIndexDataStoreV50
{
    public SqlIndexDataStoreV52(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V52;

    public override async Task EndUpdateInstanceAsync(
        int partitionKey,
        string studyInstanceUid,
        DicomDataset dicomDataset,
        IReadOnlyList<InstanceMetadata> instanceMetadataList,
        IEnumerable<QueryTag> queryTags,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        EnsureArg.IsNotNull(instanceMetadataList, nameof(instanceMetadataList));

        var filePropertiesRows = instanceMetadataList.Select(instanceMetadata
            => new FilePropertyTableTypeRow(
                instanceMetadata.InstanceProperties.NewVersion.Value,
                instanceMetadata.InstanceProperties.FileProperties.Path,
                instanceMetadata.InstanceProperties.FileProperties.ETag))
            .ToList();

        ExtendedQueryTagDataRows rows = ExtendedQueryTagDataRowsBuilder.Build(dicomDataset, queryTags, Version);
        var parameters = new VLatest.EndUpdateInstanceV52TableValuedParameters(
            filePropertiesRows,
            rows.StringRows,
            rows.LongRows,
            rows.DoubleRows,
            rows.DateTimeWithUtcRows,
            rows.PersonNameRows);

        using SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();
        VLatest.EndUpdateInstanceV52.PopulateCommand(
            sqlCommandWrapper,
            partitionKey,
            studyInstanceUid,
            dicomDataset.GetFirstValueOrDefault<string>(DicomTag.PatientID),
            dicomDataset.GetFirstValueOrDefault<string>(DicomTag.PatientName),
            dicomDataset.GetStringDateAsDate(DicomTag.PatientBirthDate),
            dicomDataset.GetFirstValueOrDefault<string>(DicomTag.ReferringPhysicianName),
            dicomDataset.GetFirstValueOrDefault<string>(DicomTag.StudyDescription),
            dicomDataset.GetFirstValueOrDefault<string>(DicomTag.AccessionNumber),
            parameters);

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
