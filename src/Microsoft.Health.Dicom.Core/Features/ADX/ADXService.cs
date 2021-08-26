// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data;
using Kusto.Data;
using Kusto.Data.Net.Client;

namespace Microsoft.Health.Dicom.Core.Features.ADX
{
    public class ADXService : IADXService
    {
        private readonly KustoConnectionStringBuilder _kustoConnectionStringBuilder;

        public ADXService(string connectionString, string clientId, string clientSecret, string dbName, string tenantID)
        {
            _kustoConnectionStringBuilder = new KustoConnectionStringBuilder($"{connectionString}/{dbName};Fed=true;")
                            .WithAadApplicationKeyAuthentication(
                                clientId,
                                clientSecret,
                                tenantID);
        }

        public DataTable ExecuteQueryAsync(string queryText)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            DataTable resulttable = new DataTable();
            var client = KustoClientFactory.CreateCslQueryProvider(_kustoConnectionStringBuilder);
#pragma warning restore CA2000 // Dispose objects before losing scope

            var reader = client.ExecuteQuery(queryText);
            resulttable.Load(reader);

            return resulttable;
        }
    }
}
