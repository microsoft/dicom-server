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
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Features.Export;
using Microsoft.Health.Dicom.Core.Exceptions;
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
        _destClient.Uri.Returns(new Uri("https://unit-test.blob.core.windows.net/mycontainer?sv=2020-08-04&ss=b", UriKind.Absolute));
        _destBlob = Substitute.For<BlobClient>();
        _errorStream = new MemoryStream();
        _errorBlob = Substitute.For<AppendBlobClient>();
        _errorBlob
            .AppendBlockAsync(Arg.Any<Stream>(), Arg.Any<AppendBlobAppendBlockOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<Response<BlobAppendInfo>>()))
            .AndDoes(c =>
            {
                object[] args = c.Args();
                var input = args[0] as Stream;
                input.CopyTo(_errorStream);
            });
        _output = new AzureBlobExportFormatOptions(
            operationId,
            "%Operation%/results/%Study%/%Series%/%SopInstance%.dcm",
            "%Operation%/errors.log");
        _destClient
            .GetAppendBlobClient($"{_output.OperationId:N}/errors.log")
            .Returns(_errorBlob);
        _blobOptions = new BlobOperationOptions
        {
            Upload = new StorageTransferOptions
            {
                MaximumConcurrency = 1,
            },
        };
        _jsonOptions = new JsonSerializerOptions();
        _jsonOptions.Converters.Add(new DicomIdentifierJsonConverter());
        _sink = new AzureBlobExportSink(_fileStore, _destClient, _output, _blobOptions, _jsonOptions);
    }

    [Fact]
    public async Task GivenValidReadResult_WhenCopying_ThenCopyToDestination()
    {
        var identifier = new VersionedInstanceIdentifier("1.2", "3.4.5", "6.7.8.9.10", 1);
        using var fileStream = new MemoryStream();
        using var tokenSource = new CancellationTokenSource();

        _fileStore.GetStreamingFileAsync(identifier.Version, identifier.Partition.Name, tokenSource.Token).Returns(fileStream);
        _destClient.GetBlobClient($"{_output.OperationId:N}/results/1.2/3.4.5/6.7.8.9.10.dcm").Returns(_destBlob);
        _destBlob
            .UploadAsync(fileStream, Arg.Is<BlobUploadOptions>(x => x.TransferOptions == _blobOptions.Upload), tokenSource.Token)
            .Returns(Task.FromResult(Substitute.For<Response<BlobContentInfo>>()));

        Assert.True(await _sink.CopyAsync(ReadResult.ForIdentifier(identifier), tokenSource.Token));

        await _fileStore.Received(1).GetStreamingFileAsync(identifier.Version, identifier.Partition.Name, tokenSource.Token);
        _destClient.Received(1).GetBlobClient($"{_output.OperationId:N}/results/1.2/3.4.5/6.7.8.9.10.dcm");
        await _destBlob
            .Received(1)
            .UploadAsync(fileStream, Arg.Is<BlobUploadOptions>(x => x.TransferOptions == _blobOptions.Upload), tokenSource.Token);
        await _errorBlob
            .DidNotReceiveWithAnyArgs()
            .AppendBlockAsync(default, default, default);
    }

    [Fact]
    public async Task GivenInvalidReadResult_WhenCopying_ThenWriteToErrorLog()
    {
        var failure = new ReadFailureEventArgs(DicomIdentifier.ForSeries("1.2.3", "4.5"), new FileNotFoundException("Cannot find series."));
        using var tokenSource = new CancellationTokenSource();

        Assert.False(await _sink.CopyAsync(ReadResult.ForFailure(failure), tokenSource.Token));

        await _fileStore.DidNotReceiveWithAnyArgs().GetStreamingFileAsync(default, default);
        _destClient.DidNotReceiveWithAnyArgs().GetBlobClient(default);
        await _destBlob.DidNotReceiveWithAnyArgs().UploadAsync(default(Stream), default(BlobUploadOptions), default);

        // Extension method appears to prevent the match
        // _destClient.Received(1).GetAppendBlobClient($"{_output.OperationId:N}/Errors.json");

        // Check errors
        ExportErrorLogEntry error = await GetErrorsAsync(tokenSource.Token).SingleAsync();
        Assert.Equal(DicomIdentifier.ForSeries("1.2.3", "4.5"), error.Identifier);
        Assert.Equal("Cannot find series.", error.Error);
    }

    [Fact]
    public async Task GivenCopyFailure_WhenCopying_ThenWriteToErrorLog()
    {
        var identifier = new VersionedInstanceIdentifier("1.2", "3.4.5", "6.7.8.9.10", 1);
        using var fileStream = new MemoryStream();
        using var tokenSource = new CancellationTokenSource();

        _fileStore.GetStreamingFileAsync(identifier.Version, identifier.Partition.Name, tokenSource.Token).Returns(fileStream);
        _destClient.GetBlobClient($"{_output.OperationId:N}/results/1.2/3.4.5/6.7.8.9.10.dcm").Returns(_destBlob);
        _destBlob
            .UploadAsync(fileStream, Arg.Is<BlobUploadOptions>(x => x.TransferOptions == _blobOptions.Upload), tokenSource.Token)
            .Returns(Task.FromException<Response<BlobContentInfo>>(new IOException("Unable to copy.")));

        Assert.False(await _sink.CopyAsync(ReadResult.ForIdentifier(identifier), tokenSource.Token));

        await _fileStore.Received(1).GetStreamingFileAsync(identifier.Version, identifier.Partition.Name, tokenSource.Token);
        _destClient.Received(1).GetBlobClient($"{_output.OperationId:N}/results/1.2/3.4.5/6.7.8.9.10.dcm");
        await _destBlob
            .Received(1)
            .UploadAsync(fileStream, Arg.Is<BlobUploadOptions>(x => x.TransferOptions == _blobOptions.Upload), tokenSource.Token);

        // Extension method appears to prevent the match
        // _destClient.Received(1).GetAppendBlobClient($"{_output.OperationId:N}/Errors.json");

        // Check errors
        ExportErrorLogEntry error = await GetErrorsAsync(tokenSource.Token).SingleAsync();
        Assert.Equal(DicomIdentifier.ForInstance("1.2", "3.4.5", "6.7.8.9.10"), error.Identifier);
        Assert.Equal("Unable to copy.", error.Error);
    }

    [Fact]
    public async Task GivenMissingContainer_WhenInitializing_ThenThrow()
    {
        using var tokenSource = new CancellationTokenSource();

        Response<bool> response = Substitute.For<Response<bool>>();
        response.Value.Returns(false);
        _destClient.ExistsAsync(tokenSource.Token).Returns(Task.FromResult(response));

        await Assert.ThrowsAsync<SinkInitializationFailureException>(() => _sink.InitializeAsync(tokenSource.Token));

        await _destClient.Received(1).ExistsAsync(tokenSource.Token);
        await _errorBlob
            .DidNotReceiveWithAnyArgs()
            .AppendBlockAsync(default, default, default);
    }

    [Theory]
    [InlineData(nameof(AggregateException))]
    [InlineData(nameof(AuthenticationFailedException))]
    [InlineData(nameof(RequestFailedException))]
    public async Task GivenThrownException_WhenInitializing_ThenWrapAndThrow(string exception)
    {
        using var tokenSource = new CancellationTokenSource();

        Response<bool> response = Substitute.For<Response<bool>>();
        response.Value.Returns(false);

        // Note: It's more likely the permissions response would be thrown when trying to append to the blob
        _destClient.ExistsAsync(tokenSource.Token).Returns(
            Task.FromException<Response<bool>>(exception switch
            {
                nameof(AggregateException) => new AggregateException(new RequestFailedException("Connection Failed")),
                nameof(AuthenticationFailedException) => new AuthenticationFailedException("Invalid tenant."),
                _ => new RequestFailedException("Insufficient Permissions"),
            }));

        await Assert.ThrowsAsync<SinkInitializationFailureException>(() => _sink.InitializeAsync(tokenSource.Token));

        await _destClient.Received(1).ExistsAsync(tokenSource.Token);
        await _errorBlob
            .DidNotReceiveWithAnyArgs()
            .AppendBlockAsync(default, default, default);
    }

    [Fact]
    public async Task GivenValidContainer_WhenInitializing_ThenCreateErrorLog()
    {
        using var tokenSource = new CancellationTokenSource();

        var errorHref = new Uri($"https://unit-test.blob.core.windows.net/mycontainer/{_output.OperationId:N}/errors.log");
        Response<bool> response = Substitute.For<Response<bool>>();
        response.Value.Returns(true);
        _destClient.ExistsAsync(tokenSource.Token).Returns(Task.FromResult(response));
        _errorBlob
            .CreateIfNotExistsAsync(default, default, tokenSource.Token)
            .Returns(Substitute.For<Response<BlobContentInfo>>());
        _errorBlob
            .Uri
            .Returns(errorHref);

        Assert.Equal(errorHref, await _sink.InitializeAsync(tokenSource.Token));

        await _destClient.Received(1).ExistsAsync(tokenSource.Token);
        await _errorBlob.Received(1).CreateIfNotExistsAsync(default, default, tokenSource.Token);

        // Extension method appears to prevent the match
        // _destClient.Received(1).GetAppendBlobClient($"{_output.OperationId:N}/Errors.json");
    }

    [Fact]
    public async Task GivenErrors_WhenFlushing_ThenAppendBlock()
    {
        using var tokenSource = new CancellationTokenSource();

        var failure1 = new ReadFailureEventArgs(DicomIdentifier.ForStudy("1.2.3"), new StudyNotFoundException("Cannot find study."));
        var failure2 = new ReadFailureEventArgs(DicomIdentifier.ForSeries("45.6", "789.10"), new SeriesNotFoundException("Cannot find series."));
        var failure3 = new ReadFailureEventArgs(DicomIdentifier.ForInstance("1.1.12", "1.3.14151", "617"), new InstanceNotFoundException("Cannot find instance."));

        Assert.False(await _sink.CopyAsync(ReadResult.ForFailure(failure1), tokenSource.Token));
        Assert.False(await _sink.CopyAsync(ReadResult.ForFailure(failure2), tokenSource.Token));
        Assert.False(await _sink.CopyAsync(ReadResult.ForFailure(failure3), tokenSource.Token));

        // Nothing read yet
        Assert.Equal(0, _errorStream.Position);

        List<ExportErrorLogEntry> errors = await GetErrorsAsync(tokenSource.Token).ToListAsync(tokenSource.Token);
        Assert.Equal(3, errors.Count);
        Assert.Equal(failure1.Exception.Message, errors[0].Error);
        Assert.Equal(failure2.Exception.Message, errors[1].Error);
        Assert.Equal(failure3.Exception.Message, errors[2].Error);
    }

    public async ValueTask DisposeAsync()
    {
        await _sink.DisposeAsync();
        _errorStream.Dispose();
    }

    private async IAsyncEnumerable<ExportErrorLogEntry> GetErrorsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _sink.FlushAsync(cancellationToken);

        await _errorBlob
            .Received(1)
            .AppendBlockAsync(Arg.Any<Stream>(), null, cancellationToken);

        _errorStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_errorStream, Encoding.UTF8);

        string line;
        while ((line = reader.ReadLine()) != null)
        {
            yield return JsonSerializer.Deserialize<ExportErrorLogEntry>(line, _jsonOptions);
        }
    }
}
