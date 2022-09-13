// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using FellowOakDicom;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Client;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.IO;


namespace DicomImageGeneratorCli;

internal static class Program
{
    private static readonly RecyclableMemoryStreamManager RecyclableMemoryStreamManager = new();

    private static readonly HttpClient Client = new();

    private static void Main(string[] args)
    {
#pragma warning disable CA1303
        string newuid = Write(rows: 65535, columns: 10000, frames: 1);

        Read(filename: $"{newuid}.dcm");
#pragma warning restore CA1303
    }

    private static string Write(int rows, int columns, int frames)
    {
        // ReSharper disable once IntVariableOverflowInUncheckedContext
        Console.WriteLine($"{rows} rows, {columns} columns, {frames} frames");
        DicomFile dicomFile = Samples.CreateRandomDicomFileWith16BitPixelData(
            rows: rows,
            columns: columns,
            frames: frames);
        string uid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
        string newuid = string.Concat(uid.AsSpan(0, uid.Length - 4), "0001");
        dicomFile.Dataset.AddOrUpdate<string>(DicomTag.StudyInstanceUID, newuid);
        Console.WriteLine(dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID));
        dicomFile.Save($@"C:\dev\hls\dicom-server\tools\DicomImageGeneratorCli\files\{newuid}.dcm");
        return newuid;
    }

    private static void Read(string filename)
    {
        // ensure saved file has right uid
        DicomFile checkFile = DicomFile.Open($@"C:\dev\hls\dicom-server\tools\DicomImageGeneratorCli\files\{filename}");
        Console.WriteLine(checkFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID));
        Console.WriteLine($"File has {checkFile.Dataset.GetSingleValue<string>(DicomTag.Rows)} rows");
        Console.WriteLine($"File has {checkFile.Dataset.GetSingleValue<string>(DicomTag.Columns)} columns");
        Console.WriteLine($"File has {checkFile.Dataset.GetSingleValue<string>(DicomTag.NumberOfFrames)} frames");
        PostFile(checkFile);
    }


    private static void PostFile(DicomFile dicomFile)
    {
        string token = "";
        var postContent = new List<Stream>();

        try
        {
            MemoryStream stream = RecyclableMemoryStreamManager.GetStream();
            Task saveTask = dicomFile.SaveAsync(stream);
            saveTask.Wait();
            stream.Seek(0, SeekOrigin.Begin);
            postContent.Add(stream);


            using MultipartContent content = ConvertStreamsToMultipartContent(postContent);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri("https://dicom-paas-perf.dicom.ci.workspace.mshapis.com/v1/studies"));

            request.Headers.Accept.Add(DicomWebConstants.MediaTypeApplicationDicomJson);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = content;
            Client.Timeout = TimeSpan.FromMinutes(10);
            Task<HttpResponseMessage> responseTask = Client.SendAsync(request, cancellationToken: default);

            HttpResponseMessage response = responseTask.Result;

            Console.WriteLine(response.StatusCode);
            Console.WriteLine(response.ReasonPhrase);
        }
        finally
        {
            // foreach (Stream stream in postContent)
            // {
            //     stream.DisposeAsync();
            // }
        }
    }
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "Callers will dispose of the StreamContent")]
    private static MultipartContent ConvertStreamsToMultipartContent(IEnumerable<Stream> streams)
    {
        var multiContent = new MultipartContent("related");

        multiContent.Headers.ContentType?.Parameters.Add(new NameValueHeaderValue("type",
            $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

        foreach (Stream stream in streams)
        {
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicom;
            multiContent.Add(streamContent);
        }

        return multiContent;
    }
}
