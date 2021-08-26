// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Cohort;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.Cohort
{
    public class CohortQueryStore : ICohortQueryStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;

        public CohortQueryStore(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
        }

        public async Task AddCohortResources(CohortData cohortData, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(cohortData, nameof(cohortData));

            foreach (var cohortResource in cohortData.CohortResources)
            {
                using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
                using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
                {
                    sqlCommandWrapper.CommandText = $"INSERT INTO dbo.Cohort (CohortId,ResourceId,ResourceType,ReferenceURL) VALUES ('{cohortData.CohortId}', '{cohortResource.ResourceId}', {(short)cohortResource.ResourceType}, '{cohortResource.ReferenceUrl}')";

                    try
                    {
                        await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
                    }
                    catch (SqlException e)
                    {
                        Console.WriteLine("sos:" + e.ToString());
                    }
                }
            }
        }

        public async Task<CohortData> GetCohortResources(Guid cohortId, CancellationToken cancellationToken)
        {
            var result = new CohortData();
            result.CohortId = cohortId;
            result.CohortResources = new List<CohortResource>();

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                sqlCommandWrapper.CommandText = $"SELECT ResourceId, ResourceType, ReferenceURL FROM dbo.Cohort WHERE CohortId = '{cohortId}'";

                Microsoft.Data.SqlClient.SqlDataReader reader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var cohortResource = new CohortResource();
                    cohortResource.ResourceId = reader.GetString(0);
                    cohortResource.ResourceType = (CohortResourceType)reader.GetInt16(1);
                    cohortResource.ReferenceUrl = reader.GetString(2);

                    result.CohortResources.Add(cohortResource);
                }
            }

            return result;
        }
    }
}
