// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<SqlCustomTagStore> _logger;

        public SqlCustomTagStore(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           SchemaInformation schemaInformation,
           ILogger<SqlCustomTagStore> logger)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
            _schemaInformation = schemaInformation;
            _logger = logger;
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

                VLatest.AddCustomTags.PopulateCommand(sqlCommandWrapper, new VLatest.AddCustomTagsTableValuedParameters(rows));

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

        public async Task<IReadOnlyList<CustomTagStoreEntry>> GetCustomTagsAsync(string path, CancellationToken cancellationToken = default)
        {
            if (_schemaInformation.Current < SchemaVersionConstants.SupportCustomTagSchemaVersion)
            {
                throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
            }

            List<CustomTagStoreEntry> results = new List<CustomTagStoreEntry>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.GetCustomTag.PopulateCommand(sqlCommandWrapper, path);

                var executionTimeWatch = Stopwatch.StartNew();
                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (long tagKey, string tagPath, string tagVR, int tagLevel, int tagStatus) = reader.ReadRow(
                            VLatest.CustomTag.TagKey,
                            VLatest.CustomTag.TagPath,
                            VLatest.CustomTag.TagVR,
                            VLatest.CustomTag.TagLevel,
                            VLatest.CustomTag.TagStatus);

                        results.Add(new CustomTagStoreEntry(tagKey, tagPath, tagVR, (CustomTagLevel)tagLevel, (CustomTagStatus)tagStatus));
                    }

                    executionTimeWatch.Stop();
                    _logger.LogInformation(executionTimeWatch.ElapsedMilliseconds.ToString());
                }
            }

            return results.AsReadOnly();
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
