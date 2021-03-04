// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.CustomTag
{
    public class SqlCustomTagStore : ICustomTagStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
        private readonly SchemaInformation _schemaInformation;

        public SqlCustomTagStore(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           SchemaInformation schemaInformation)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
            _schemaInformation = schemaInformation;
        }

        public async Task AddCustomTagsAsync(IEnumerable<CustomTagEntry> customTagEntries, CancellationToken cancellationToken = default)
        {
            if (_schemaInformation.Current < SchemaVersionConstants.SupportCustomTagSchemaVersion)
            {
                throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
            }

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                IEnumerable<AddCustomTagsInputTableTypeV1Row> rows = customTagEntries.Select(ToAddCustomTagsInputTableTypeV1Row);
                V2.AddCustomTags.PopulateCommand(sqlCommandWrapper, new V2.AddCustomTagsTableValuedParameters(rows));

                try
                {
                    await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.Conflict:
                            throw new CustomTagsAlreadyExistsException();

                        default:
                            throw new DataStoreException(ex);
                    }
                }
            }
        }

        public async Task<IReadOnlyCollection<CustomTagEntry>> GetCustomTagsAsync(string path, CancellationToken cancellationToken)
        {
            if (_schemaInformation.Current < SchemaVersionConstants.SupportCustomTagSchemaVersion)
            {
                throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
            }

            List<CustomTagEntry> results = new List<CustomTagEntry>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                V2.GetCustomTag.PopulateCommand(sqlCommandWrapper, path);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (string tagPath, string tagVR, int tagLevel, int tagStatus) = reader.ReadRow(
                           V2.CustomTag.TagPath,
                           V2.CustomTag.TagVR,
                           V2.CustomTag.TagLevel,
                           V2.CustomTag.TagStatus);

                        results.Add(new CustomTagEntry { Path = tagPath, VR = tagVR, Level = (CustomTagLevel)tagLevel, Status = (CustomTagStatus)tagStatus });
                    }
                }
            }

            return results;
        }

        private static AddCustomTagsInputTableTypeV1Row ToAddCustomTagsInputTableTypeV1Row(CustomTagEntry entry)
        {
            return new AddCustomTagsInputTableTypeV1Row(entry.Path, entry.VR, (byte)entry.Level);
        }

        public async Task DeleteCustomTagAsync(string tagPath, string vr, CancellationToken cancellationToken = default)
        {
            if (_schemaInformation.Current < SchemaVersionConstants.SupportCustomTagSchemaVersion)
            {
                throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
            }

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.DeleteCustomTag.PopulateCommand(sqlCommandWrapper, tagPath, (byte)CustomTagLimit.CustomTagVRAndDataTypeMapping[vr]);

                try
                {
                    await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.NotFound:
                            throw new CustomTagNotFoundException(
                                string.Format(CultureInfo.InvariantCulture, DicomSqlServerResource.CustomTagNotFound, tagPath));
                        case SqlErrorCodes.PreconditionFailed:
                            throw new CustomTagBusyException(
                                string.Format(CultureInfo.InvariantCulture, DicomSqlServerResource.CustomTagIsBusy, tagPath));
                        default:
                            throw new DataStoreException(ex);
                    }
                }
            }
        }
    }
}
