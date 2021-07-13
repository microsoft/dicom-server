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
    internal class SqlExtendedQueryTagStoreV4 : SqlExtendedQueryTagStoreV3
    {
        public SqlExtendedQueryTagStoreV4(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           ILogger<SqlExtendedQueryTagStoreV4> logger)
            : base(sqlConnectionWrapperFactory, logger)
        {
        }

        public override VersionRange SupportedVersions => SchemaVersion.V4;

        public override async Task<IReadOnlyList<int>> AddExtendedQueryTagsAsync(
            IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries,
            int maxAllowedCount,
            bool ready = false,
            CancellationToken cancellationToken = default)
        {
            using SqlConnectionWrapper sqlConnectionWrapper = await ConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
            using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand();

            IEnumerable<AddExtendedQueryTagsInputTableTypeV1Row> rows = extendedQueryTagEntries.Select(ToAddExtendedQueryTagsInputTableTypeV1Row);
            VLatest.AddExtendedQueryTags.PopulateCommand(sqlCommandWrapper, maxAllowedCount, ready, new VLatest.AddExtendedQueryTagsTableValuedParameters(rows));

            try
            {
                var keys = new List<int>();
                using SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    keys.Add(reader.ReadRow(VLatest.ExtendedQueryTagString.TagKey));
                }

                return keys;
            }
            catch (SqlException ex)
            {
                throw ex.Number switch
                {
                    SqlErrorCodes.Conflict => ex.State == 1
                        ? new ExtendedQueryTagsExceedsMaxAllowedCountException(maxAllowedCount)
                        : new ExtendedQueryTagsAlreadyExistsException(),
                    _ => new DataStoreException(ex),
                };
            }
        }
    }
}
