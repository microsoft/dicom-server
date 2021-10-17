// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.SqlServer.Features.Schema;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    /// <summary>
    /// Basic performance tests to detect regression
    /// </summary>
    public class PerformanceTests
    {
        [Fact]
        public async Task CurrentAndLastSchemaVersions_HaveSimilarStorePerformance()
        {
            var current = SchemaVersionConstants.Max;
            var previous = SchemaVersionConstants.Max - 1;

            var currentSchemaInformation = new SchemaInformation(SchemaVersionConstants.Min, current);
            currentSchemaInformation.Current = current;

            var previousSchemaInformation = new SchemaInformation(SchemaVersionConstants.Min, previous);
            previousSchemaInformation.Current = previous;

            var fixtures = new Dictionary<int, SqlDataStoreTestsFixture>
            {
                { current, new SqlDataStoreTestsFixture(SqlDataStoreTestsFixture.GenerateDatabaseName("PERFCURRENT"), currentSchemaInformation) },
                { previous, new SqlDataStoreTestsFixture(SqlDataStoreTestsFixture.GenerateDatabaseName("PERFPREVIOUS"), previousSchemaInformation) }
            };

            var schemaVersionPerformance = new Dictionary<int, TimeSpan>();

            foreach ((var version, var fixture) in fixtures)
            {
                var stopwatch = Stopwatch.StartNew();

                for (var i = 0; i < 10000; i++)
                {
                    var dataset = Samples.CreateRandomInstanceDataset();
                    await fixture.IndexDataStore.BeginCreateInstanceIndexAsync(dataset, null);
                }

                stopwatch.Stop();
                schemaVersionPerformance.Add(version, stopwatch.Elapsed);
            }

            var diff = schemaVersionPerformance[current] - schemaVersionPerformance[previous];
            var marginOfError = schemaVersionPerformance[previous] * 0.02;

            Assert.True(diff < marginOfError);
        }

        private async Task<IReadOnlyList<int>> AddExtendedQueryTagsAsync(
            SqlDataStoreTestsFixture fixture,
            IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries,
            int maxAllowedCount = 128,
            bool ready = true,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ExtendedQueryTagStoreEntry> tags = await fixture.ExtendedQueryTagStore.AddExtendedQueryTagsAsync(
                extendedQueryTagEntries,
                maxAllowedCount,
                ready: ready,
                cancellationToken: cancellationToken);

            return tags.Select(x => x.Key).ToList();
        }
    }
}
