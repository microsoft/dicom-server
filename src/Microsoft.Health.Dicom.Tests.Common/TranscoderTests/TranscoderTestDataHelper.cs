// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace Microsoft.Health.Dicom.Tests.Common.TranscoderTests
{
    public static class TranscoderTestDataHelper
    {
        private const string AllFiles = "*";
        private const string ExpectedOutputFileName = "ExpectedOutput.dcm";
        private const string MetadataFileName = "Metadata.json";
        private const string InputFileName = "Input.dcm";

        private static string GetTestDataFolder(string inputFilePath)
        {
            return Path.GetDirectoryName(inputFilePath);
        }

        private static string GetExpectedOutputFile(string testDataFolder)
        {
            return Path.Combine(testDataFolder, ExpectedOutputFileName);
        }

        private static string GetInputFile(string testDataFolder)
        {
            return Path.Combine(testDataFolder, InputFileName);
        }

        private static string GetMetadataFile(string testDataFolder)
        {
            return Path.Combine(testDataFolder, MetadataFileName);
        }

        private static bool IsInputFile(string path)
        {
            return Path.GetFileName(path).Equals(InputFileName, StringComparison.OrdinalIgnoreCase);
        }

        private static TranscoderTestMetadata GetMetadata(string testDataFolder)
        {
            string metadataFile = GetMetadataFile(testDataFolder);
            return JsonSerializer.Deserialize<TranscoderTestMetadata>(File.ReadAllText(metadataFile));
        }

        public static IEnumerable<string> GetTestDataFolders(string testDataRootFolder)
        {
            foreach (string path in Directory.EnumerateFiles(testDataRootFolder, AllFiles, SearchOption.AllDirectories))
            {
                if (IsInputFile(path))
                {
                    yield return GetTestDataFolder(path);
                }
            }
        }

        public static TranscoderTestData GetTestData(string testDataFolder)
        {
            return new TranscoderTestData()
            {
                InputDicomFile = GetInputFile(testDataFolder),
                ExpectedOutputDicomFile = GetExpectedOutputFile(testDataFolder),
                MetaData = GetMetadata(testDataFolder),
            };
        }

        public static IEnumerable<TranscoderTestData> GetTestDatas(string testDataRootFolder)
        {
            foreach (string folder in GetTestDataFolders(testDataRootFolder))
            {
                yield return GetTestData(folder);
            }
        }

        public static string GetHashFromStream(Stream byteStream)
        {
            byte[] result = ToByteArray(byteStream);
            using var sha256Managed = new SHA256Managed();
            return Convert.ToBase64String(sha256Managed.ComputeHash(result));
        }

        private static byte[] ToByteArray(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
