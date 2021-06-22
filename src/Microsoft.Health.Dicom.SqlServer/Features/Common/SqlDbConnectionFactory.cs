// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.SqlServer;

namespace Microsoft.Health.Dicom.SqlServer.Features.Common
{
    public class SqlDbConnectionFactory : IDbConnectionFactory
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public SqlDbConnectionFactory(ISqlConnectionFactory sqlConnectionFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionFactory, nameof(sqlConnectionFactory));
            _sqlConnectionFactory = sqlConnectionFactory;
        }

        public async Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            // Note: Async/Await required for polymorphism
            return await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken);
        }
    }
}
