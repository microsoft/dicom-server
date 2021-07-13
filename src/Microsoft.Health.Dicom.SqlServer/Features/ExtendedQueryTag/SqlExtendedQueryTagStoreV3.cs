// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag
{
    internal class SqlExtendedQueryTagStoreV3 : SqlExtendedQueryTagStoreV2
    {
        public SqlExtendedQueryTagStoreV3(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           ILogger<SqlExtendedQueryTagStoreV3> logger)
            : base(sqlConnectionWrapperFactory, logger)
        {
        }

        public override VersionRange SupportedVersions => SchemaVersion.V3;

        public override async Task<IReadOnlyList<int>> AddExtendedQueryTagsAsync(
            IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries,
            int maxAllowedCount,
            bool ready = false,
            CancellationToken cancellationToken = default)
        {
            if (ready)
            {
                throw new BadRequestException(DicomSqlServerResource.SchemaVersionNeedsToBeUpgraded);
            }

            using (SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                IEnumerable<AddExtendedQueryTagsInputTableTypeV1Row> rows = extendedQueryTagEntries.Select(ToAddExtendedQueryTagsInputTableTypeV1Row);

                V3.AddExtendedQueryTags.PopulateCommand(sqlCommandWrapper, rows, maxAllowedCount);

                try
                {
                    await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
                    var allTags = (await GetExtendedQueryTagsAsync(path: null, cancellationToken: cancellationToken))
                        .ToDictionary(x => x.Path, x => x.Key);

                    return extendedQueryTagEntries
                        .Select(x => allTags[x.Path])
                        .ToList();
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlErrorCodes.Conflict:
                        {
                            if (ex.State == 1)
                            {
                                throw new ExtendedQueryTagsExceedsMaxAllowedCountException(maxAllowedCount);
                            }
                            else
                            {
                                throw new ExtendedQueryTagsAlreadyExistsException();
                            }
                        }

                        default:
                            throw new DataStoreException(ex);
                    }
                }
            }
        }
    }
}
