// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.SqlServer.Features.Retrieve;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.SqlServer.Registration;
using Microsoft.Health.Blob.Configs;
using Microsoft.IO;
using Microsoft.Health.Dicom.Core.Configs;
using System.Text.Json;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Client;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using BenchmarkDotNet.Engines;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Health.Dicom.Benchmark.Retrieve;

[SimpleJob(RunStrategy.Monitoring, targetCount: 10)]
[MinColumn, Q1Column, Q3Column, MaxColumn]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class WadoBenchmark : DicomBenchmark
{
    private const int StudySize = 1000;
    private const string StudyUid = "123456.789.10";

    private readonly IServiceProvider _services;

    public WadoBenchmark()
    {
        _services = new ServiceCollection()
            .AddSingleton(Configuration)
            .AddLogging(x => x.AddConsole())
            .Configure<DicomClientOptions>(Configuration.GetSection("DicomClient"))
            .Configure<SqlServerDataStoreConfiguration>(Configuration.GetSection(SqlServerDataStoreConfiguration.SectionName))
            .Configure<BlobServiceClientOptions>(Configuration.GetSection(BlobServiceClientOptions.DefaultSectionName))
            .Configure<BlobMigrationConfiguration>(Configuration.GetSection("DicomServer:Services:BlobMigration"))
            .Configure<BlobContainerConfiguration>(Constants.MetadataContainerConfigurationName, Configuration.GetSection("DicomWeb:MetadataStore"))
            .Configure<FeatureConfiguration>(Configuration.GetSection("DicomServer:Features"))
            .Configure<JsonSerializerOptions>(o => o.ConfigureDefaultDicomSettings())
            .Configure<RetrieveConfiguration>(Configuration.GetSection("DicomServer:Services:Retrieve"))
            .Configure<LoggerFilterOptions>(Configuration.GetSection("Logging"))
            .AddBlobServiceClient(Configuration.GetSection(BlobServiceClientOptions.DefaultSectionName))
            .AddSqlServerConnection()
            .AddScoped<IDicomRequestContext>(s => new DicomRequestContext(HttpMethod.Get.Method, new Uri("http://localhost/benchmark"), new Uri("http://localhost"), Guid.NewGuid().ToString(), new Dictionary<string, StringValues>(), new Dictionary<string, StringValues>()))
            .AddScoped<IDicomRequestContextAccessor>(s => new DicomRequestContextAccessor { RequestContext = s.GetRequiredService<IDicomRequestContext>() })
            .AddSingleton<RecyclableMemoryStreamManager>()
            .AddSingleton<DicomFileNameWithUid>()
            .AddSingleton<DicomFileNameWithPrefix>()
            .AddScoped<IETagGenerator, ETagGenerator>()
            .AddScoped<IInstanceStore, SqlInstanceStoreV23>()
            .AddScoped<IMetadataStore, BlobMetadataStore>()
            .AddScoped<LegacyRetrieveMetadataService>()
            .BuildServiceProvider();
    }

    [GlobalSetup]
    public async Task SetupAsync()
    {
        DicomClientOptions options = _services.GetRequiredService<IOptions<DicomClientOptions>>().Value;
        using var httpClient = new HttpClient { BaseAddress = options.BaseAddress };
        var client = new DicomWebClient(httpClient, "v1");

        var result = await client.QueryStudyAsync($"StudyInstanceUID={StudyUid}");
        if (await result.AnyAsync())
            return; // Already uploaded

        // Upload test data
        await client.StoreAsync
            (Enumerable.Range(1, StudySize).Select(x => new DicomFile(CreateRandomInstanceDataset(StudyUid))),
            StudyUid);
    }

    [Benchmark(Baseline = true)]
    public async Task OldWado()
    {
        using IServiceScope scope = _services.CreateScope();
        LegacyRetrieveMetadataService service = scope.ServiceProvider.GetRequiredService<LegacyRetrieveMetadataService>();

        LegacyRetrieveMetadataResponse response = await service.RetrieveStudyInstanceMetadataAsync(StudyUid);
        int count = response.ResponseMetadata.Count();
        if (count != StudySize)
            throw new InvalidOperationException("Invalid study!");
    }

    [Benchmark]
    public Task NewWado10()
        => NewWado(10);

    [Benchmark]
    public Task NewWado100()
        => NewWado(100);

    [Benchmark]
    public Task NewWado999()
        => NewWado(999);

    private async Task NewWado(int parallelism)
    {
        var options = new RetrieveConfiguration { MaxBufferedDataSets = StudySize, MaxDegreeOfParallelism = parallelism };

        using IServiceScope scope = _services.CreateScope();
        var service = new RetrieveMetadataService(
            scope.ServiceProvider.GetRequiredService<IInstanceStore>(),
            scope.ServiceProvider.GetRequiredService<IMetadataStore>(),
            scope.ServiceProvider.GetRequiredService<IETagGenerator>(),
            scope.ServiceProvider.GetRequiredService<IDicomRequestContextAccessor>(),
            Options.Create(options));

        RetrieveMetadataResponse response = await service.RetrieveStudyInstanceMetadataAsync(StudyUid);
        int count = await response.ResponseMetadata.CountAsync();
        if (count != StudySize)
            throw new InvalidOperationException("Invalid study!");
    }
}
