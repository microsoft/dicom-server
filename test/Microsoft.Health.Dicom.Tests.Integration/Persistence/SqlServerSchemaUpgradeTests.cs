// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.SqlServer.Dac.Compare;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class SqlServerSchemaUpgradeTests
    {
        [Fact]
        public async Task GivenTwoSchemaInitializationMethods_WhenCreatingTwoDatabases_BothSchemasShouldBeEquivalent()
        {
            // Create two databases, one where we apply the the maximum supported version's snapshot SQL schema file
            SqlDataStoreTestsFixture snapshotFixture = new SqlDataStoreTestsFixture(SqlDataStoreTestsFixture.GenerateDatabaseName("SNAPSHOT"));

            // And one where we apply .diff.sql files to upgrade the schema version to the maximum supported version.
            SqlDataStoreTestsFixture diffFixture = new SqlDataStoreTestsFixture(SqlDataStoreTestsFixture.GenerateDatabaseName("DIFF"));

            await snapshotFixture.InitializeAsync(forceIncrementalSchemaUpgrade: false);
            await diffFixture.InitializeAsync(forceIncrementalSchemaUpgrade: true);

            SchemaCompareDatabaseEndpoint snapshotEndpoint = new SchemaCompareDatabaseEndpoint(snapshotFixture.TestConnectionString);
            SchemaCompareDatabaseEndpoint diffEndpoint = new SchemaCompareDatabaseEndpoint(diffFixture.TestConnectionString);
            var comparison = new SchemaComparison(snapshotEndpoint, diffEndpoint);

            SchemaComparisonResult result = comparison.Compare();
            Assert.True(result.IsEqual);

            // cleanup if succeeds
            await snapshotFixture.DisposeAsync();
            await diffFixture.DisposeAsync();
        }

        [Theory]
        [MemberData(nameof(SchemaDiffVersions))]
        public async Task GivenASchemaVersion_WhenApplyingDiffTwice_ShouldSucceed(int schemaVersion)
        {
            SqlDataStoreTestsFixture snapshotFixture = new SqlDataStoreTestsFixture(SqlDataStoreTestsFixture.GenerateDatabaseName("SNAPSHOT"));
            snapshotFixture.SchemaInformation = new SchemaInformation(SchemaVersionConstants.Min, schemaVersion - 1);

            await snapshotFixture.InitializeAsync(forceIncrementalSchemaUpgrade: false);
            await snapshotFixture.SchemaUpgradeRunner.ApplySchemaAsync(schemaVersion, applyFullSchemaSnapshot: false, CancellationToken.None);
            await snapshotFixture.SchemaUpgradeRunner.ApplySchemaAsync(schemaVersion, applyFullSchemaSnapshot: false, CancellationToken.None);

            // cleanup if succeeds
            await snapshotFixture.DisposeAsync();
        }

        public static IEnumerable<object[]> SchemaDiffVersions = new List<object[]>(Enumerable.Range(SchemaVersionConstants.Min + 1, SchemaVersionConstants.Max - 1).Select(x => new object[] { x }));
    }
}
