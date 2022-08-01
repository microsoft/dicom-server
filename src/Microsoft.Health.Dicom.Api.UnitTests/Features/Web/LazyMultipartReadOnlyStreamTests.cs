// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Api.Web;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Web;

/// <summary>
/// Validates our LazyMultipartReadOnlyStream output with Asp.net frameworks MultipartContent output
/// </summary>
public class LazyMultipartReadOnlyStreamTests
{
    private const string MediaType = "application/octet-stream";
    private const string MultipartContentSubType = "related";
    private const string TransferSyntax = "transfer-syntax";
    private const string TestTransferSyntaxUid = "1.2.840.10008.1.2.1";
    private const string ContentType = "Content-Type";

    [Theory]
    [InlineData(1024, 2048)]
    [InlineData(2048, 2048)]
    [InlineData(4096, 2048)]
    public async Task GivenSingleStream_WhenMultiPartRequested_StreamContentIsCorrect(int streamSize, int bufferSize)
    {
        // arrange
        string boundary = Guid.NewGuid().ToString();
        using Stream originalStream = GetRandomStream(streamSize);
        using MemoryStream expectedMemoryStream = await GetMultipartContentStreamAsync(new[] { originalStream }, boundary);

        // act
        using Stream lazyStream = new LazyMultipartReadOnlyStream(
            GetAsyncEnumerable(new[] { originalStream }),
            boundary,
            bufferSize,
            CancellationToken.None);
        using MemoryStream actualMemoryStream = new MemoryStream();
        await lazyStream.CopyToAsync(actualMemoryStream);

        // assert
        Assert.True(ValidateStreamContent(actualMemoryStream, expectedMemoryStream));
    }

    [Theory]
    [InlineData(1024, 2048)]
    [InlineData(2048, 2048)]
    [InlineData(4096, 2048)]
    public async Task GivenMultipleStreams_WhenMultiPartRequested_StreamContentIsCorrect(int streamSize, int bufferSize)
    {
        // arrange
        string boundary = Guid.NewGuid().ToString();
        using Stream originalStream1 = GetRandomStream(streamSize);
        using Stream originalStream2 = GetRandomStream(streamSize);
        using MemoryStream expectedMemoryStream = await GetMultipartContentStreamAsync(new[] { originalStream1, originalStream2 }, boundary);

        // act
        using Stream lazyStream = new LazyMultipartReadOnlyStream(
            GetAsyncEnumerable(new[] { originalStream1, originalStream2 }),
            boundary,
            bufferSize,
            CancellationToken.None);
        using MemoryStream actualMemoryStream = new MemoryStream();
        await lazyStream.CopyToAsync(actualMemoryStream);

        // assert
        Assert.True(ValidateStreamContent(actualMemoryStream, expectedMemoryStream));
    }

    private static bool ValidateStreamContent(MemoryStream actualStream, MemoryStream expectedStream)
    {
        if (actualStream.Length != expectedStream.Length)
        {
            return false;
        }
        actualStream.Position = 0;
        expectedStream.Position = 0;

        var msArray1 = actualStream.ToArray();
        var msArray2 = expectedStream.ToArray();

        return msArray1.SequenceEqual(msArray2);
    }

    private static async Task<MemoryStream> GetMultipartContentStreamAsync(Stream[] originalStreams, string boundary)
    {
        var content = new MultipartContent(MultipartContentSubType, boundary);
        var mediaType = new MediaTypeHeaderValue(MediaType);
        mediaType.Parameters.Add(new NameValueHeaderValue(TransferSyntax, TestTransferSyntaxUid));

        foreach (Stream item in originalStreams)
        {
            var streamContent = new StreamContent(item);
            streamContent.Headers.ContentType = mediaType;
            content.Add(streamContent);
        }

        MemoryStream stream = new MemoryStream();
        await content.CopyToAsync(stream);
        return stream;
    }

    private static Stream GetRandomStream(long size, Random random = null)
    {
        random ??= new Random(Environment.TickCount);
        var buffer = new byte[size];
        random.NextBytes(buffer);
        return new MemoryStream(buffer);
    }

    private static async IAsyncEnumerable<DicomStreamContent> GetAsyncEnumerable(Stream[] streams)
    {
        List<KeyValuePair<string, IEnumerable<string>>> headers = new List<KeyValuePair<string, IEnumerable<string>>>();
        headers.Add(new KeyValuePair<string, IEnumerable<string>>(ContentType, new List<string> { $"{MediaType}; {TransferSyntax}={TestTransferSyntaxUid}" }));

        await Task.Run(() => 1);
        foreach (var stream in streams)
        {
            stream.Position = 0;
            yield return new DicomStreamContent()
            {
                Stream = stream,
                Headers = headers,
                StreamLength = stream.Length,
            };
        }
    }
}
