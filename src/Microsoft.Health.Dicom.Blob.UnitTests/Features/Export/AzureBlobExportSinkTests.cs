// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models.Common;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Core.Serialization;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests.Features.Export;

public class AzureBlobExportSinkTests : IAsyncDisposable
{
    private readonly IFileStore _fileStore;
    private readonly BlobContainerClient _destClient;
    private readonly BlobClient _destBlob;
    private readonly AppendBlobClient _errorBlob;
    private readonly MemoryStream _errorStream;
    private readonly AzureBlobExportFormatOptions _output;
    private readonly BlobOperationOptions _blobOptions;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly AzureBlobExportSink _sink;

    public AzureBlobExportSinkTests()
    {
        var operationId = Guid.NewGuid();

        _fileStore = Substitute.For<IFileStore>();
        _destClient = Substitute.For<BlobContainerClient>();
        _destBlob = Substitute.For<BlobClient>();
        _errorStream = new MemoryStream();
        _errorBlob = Substitute.For<AppendBlobClient>();
        _errorBlob
            .AppendBlockAsync(Arg.Any<Stream>(), Arg.Any<byte[]>(), Arg.Any<AppendBlobRequestConditions>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<Response<BlobAppendInfo>>()))
            .AndDoes(c =>
            {
                object[] args = c.Args();
                var input = args[0] as Stream;
                input.CopyTo(_errorStream);
            });
        _output = new AzureBlobExportFormatOptions(
            operationId,
            "%Operation%/Results/%Study%/%Series%/%SopInstance%.dcm",
            "%Operation%/Errors.json",
            Encoding.UTF8);
        _blobOptions = new BlobOperationOptions
        {
            Upload = new StorageTransferOptions
            {
                MaximumConcurrency = 1,
            },
        };
        _jsonOptions = new JsonSerializerOptions();
        _jsonOptions.Converters.Add(new DicomIdentifierJsonConverter());
        _sink = new AzureBlobExportSink(_fileStore, _destClient, Options.Create(_output), Options.Create(_blobOptions), Options.Create(_jsonOptions));
    }

    [Fact]
    public async Task GivenValidReadResult_WhenCopying_ThenCopyToDestination()
    {
        var identifier = new VersionedInstanceIdentifier("1.2", "3.4.5", "6.7.8.9.10", 1);
        using var fileStream = new MemoryStream();
        using var tokenSource = new CancellationTokenSource();

        _fileStore.GetFileAsync(identifier, tokenSource.Token).Returns(fileStream);
        _destClient.GetBlobClient($"{_output.OperationId:N}/Results/1.2/3.4.5/6.7.8.9.10.dcm").Returns(_destBlob);
        _destBlob
            .UploadAsync(fileStream, Arg.Is<BlobUploadOptions>(x => x.TransferOptions == _blobOptions.Upload), tokenSource.Token)
            .Returns(Task.FromResult(Substitute.For<Response<BlobContentInfo>>()));

        Assert.True(await _sink.CopyAsync(ReadResult.ForIdentifier(identifier), tokenSource.Token));

        await _fileStore.Received(1).GetFileAsync(identifier, tokenSource.Token);
        _destClient.Received(1).GetBlobClient($"{_output.OperationId:N}/Results/1.2/3.4.5/6.7.8.9.10.dcm");
        await _destBlob
            .Received(1)
            .UploadAsync(fileStream, Arg.Is<BlobUploadOptions>(x => x.TransferOptions == _blobOptions.Upload), tokenSource.Token);
    }

    [Fact]
    public async Task GivenInvalidReadResult_WhenCopying_ThenCopyToDestination()
    {
        var failure = new ReadFailureEventArgs(DicomIdentifier.ForSeries("1.2.3", "4.5"), new FileNotFoundException("Cannot find series."));
        using var tokenSource = new CancellationTokenSource();

        _destClient.GetAppendBlobClient(default).ReturnsForAnyArgs(_errorBlob);
        Assert.False(await _sink.CopyAsync(ReadResult.ForFailure(failure), tokenSource.Token));

        // Check errors
        ExportErrorLogEntry error = await GetErrorsAsync().SingleAsync();
        Assert.Equal(DicomIdentifier.ForSeries("1.2.3", "4.5"), error.Identifier);
        Assert.Equal("Cannot find series.", error.Error);
    }

    public async ValueTask DisposeAsync()
    {
        await _sink.DisposeAsync();
        _errorStream.Dispose();
    }

    private async IAsyncEnumerable<ExportErrorLogEntry> GetErrorsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _sink.FlushErrorsAsync(cancellationToken);

        _errorStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_errorStream, Encoding.UTF8);

        string line;
        while ((line = reader.ReadLine()) != null)
        {
            yield return JsonSerializer.Deserialize<ExportErrorLogEntry>(line, _jsonOptions);
        }
    }
}
