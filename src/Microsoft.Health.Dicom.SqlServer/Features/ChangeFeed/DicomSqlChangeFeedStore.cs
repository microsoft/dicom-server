// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed
{
    public class DicomSqlChangeFeedStore : IDicomChangeFeedStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
        private readonly ILogger<DicomSqlChangeFeedStore> _logger;

        public DicomSqlChangeFeedStore(
           SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
           ILogger<DicomSqlChangeFeedStore> logger)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
            _logger = logger;
        }

        public async Task<ChangeFeedEntry> GetChangeFeedLatestAsync(CancellationToken cancellationToken)
        {
            // TODO implement
            _logger.LogError("not implemented");
            return await Task.FromResult(new List<ChangeFeedEntry>().First());
        }

        public async Task<IReadOnlyCollection<ChangeFeedEntry>> GetChangeFeedAsync(int offset, int limit, CancellationToken cancellationToken)
        {
            // TODO implement
            _logger.LogError("not implemented");
            return await Task.FromResult(new List<ChangeFeedEntry>());
        }
    }
}
