// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class ExtendedQueryTagErrorStoreTestHelper : IExtendedQueryTagErrorStoreTestHelper
    {
        private readonly string _connectionString;

        public ExtendedQueryTagErrorStoreTestHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Task ClearExtendedQueryTagErrorTableAsync()
        {
            return SqlTestUtils.ClearTableAsync(_connectionString, VLatest.ExtendedQueryTagError.TableName);
        }
    }
}
