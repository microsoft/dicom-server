// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.SqlServer.Features.Schema;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    /// <summary>
    /// Basic performance tests to detect regression
    /// </summary>
    public class PerformanceTests
    {
        private readonly int _currentSchemaVersion = SchemaVersionConstants.Max;
        private readonly int _previousSchemaVersion = SchemaVersionConstants.Max - 1;
        private readonly Dictionary<int, SqlDataStoreTestsFixture> _fixtures = new Dictionary<int, SqlDataStoreTestsFixture>();
        private readonly ITestOutputHelper _testOutputHelper;

        private readonly Random _rng = new Random();

        private const int CreateInstances = 5000;
        private readonly List<DicomDataset> _instances = new List<DicomDataset>();

        public PerformanceTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = EnsureArg.IsNotNull(testOutputHelper);
        }


        [Fact]
        public async Task CurrentAndLastSchemaVersions_HaveSimilarStorePerformance()
        {
            await InitializeFixtures();


            for (var i = 0; i < CreateInstances; i++)
            {
                var dataset = Samples.CreateRandomInstanceDataset();
                _instances.Add(dataset);
            }

            await RunTest(
                "IndexDataStore",
                "BeginCreateInstanceIndexAsync",
                5000,
                x => new object[] { _instances[x], new List<QueryTag>(), null });

            await RunTest(
                "InstanceStore",
                "GetInstanceIdentifiersInSeriesAsync",
                10000,
                x =>
                {
                    var instanceIdentifier = _instances[_rng.Next(CreateInstances)].ToInstanceIdentifier();
                    return new object[] { instanceIdentifier.StudyInstanceUid, instanceIdentifier.SeriesInstanceUid, null };
                });

            await RunTest(
                "IndexDataStore",
                "DeleteStudyIndexAsync",
                500,
                x =>
                {
                    var instanceIdentifier = _instances[_rng.Next(CreateInstances)].ToInstanceIdentifier();
                    return new object[] { instanceIdentifier.StudyInstanceUid, DateTimeOffset.UtcNow, null };
                });
        }

        private async Task RunTest(string storeStr, string methodStr, int calls, Func<int, object[]> createArgs)
        {

            var schemaVersionPerformance = new Dictionary<int, decimal>();

            foreach ((var version, var fixture) in _fixtures)
            {
                PropertyInfo storeInfo = fixture.GetType().GetProperty(storeStr);
                object store = storeInfo.GetValue(fixture);
                MethodInfo methodInfo = storeInfo.PropertyType.GetMethod(methodStr);

                var stopwatch = Stopwatch.StartNew();

                for (var i = 0; i < calls; i++)
                {
                    try
                    {
                        await (Task)methodInfo.Invoke(store, createArgs(i));
                    }
                    catch (StudyNotFoundException)
                    {
                    }
                }
                stopwatch.Stop();
                schemaVersionPerformance.Add(version, stopwatch.ElapsedMilliseconds);
            }

            var diff = schemaVersionPerformance[_currentSchemaVersion] - schemaVersionPerformance[_previousSchemaVersion];

            var diffPercentage = diff / schemaVersionPerformance[_currentSchemaVersion];

            _testOutputHelper.WriteLine("==================================================");
            _testOutputHelper.WriteLine($"{storeStr}.{methodStr} - Average Duration (ms)");
            _testOutputHelper.WriteLine($"{ _previousSchemaVersion}: { schemaVersionPerformance[_previousSchemaVersion] / calls}, "
                + $"{_currentSchemaVersion}: {schemaVersionPerformance[_currentSchemaVersion] / calls}");
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

        private async Task InitializeFixtures()
        {
            var currentSchemaInformation = new SchemaInformation(SchemaVersionConstants.Min, _currentSchemaVersion);
            var previousSchemaInformation = new SchemaInformation(SchemaVersionConstants.Min, _previousSchemaVersion);

            var currentFixture = new SqlDataStoreTestsFixture(SqlDataStoreTestsFixture.GenerateDatabaseName("PERFCURRENT"), currentSchemaInformation);
            var previousFixture = new SqlDataStoreTestsFixture(SqlDataStoreTestsFixture.GenerateDatabaseName("PERFPREVIOUS"), previousSchemaInformation);

            await currentFixture.InitializeAsync(false);
            await previousFixture.InitializeAsync(false);

            _fixtures.Add(_previousSchemaVersion, previousFixture);
            _fixtures.Add(_currentSchemaVersion, currentFixture);
        }
    }
}
