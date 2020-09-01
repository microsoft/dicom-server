// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Microsoft.Health.Dicom.Tests.Common.TranscoderTests
{
    public static class TranscoderTestDataHelper
    {
        private const string AllFiles = "*";
        private const string ExpectedOutputFileName = "ExpectedOutput.dcm";
        private const string MetadataFileName = "Metadata.json";
        private const string InputFileName = "Input.dcm";

        private static string GetExpectedOutputFile(string inputFile)
        {
            return Path.Combine(Path.GetDirectoryName(inputFile), ExpectedOutputFileName);
        }

        private static string GetMetadataFile(string inputFile)
        {
            return Path.Combine(Path.GetDirectoryName(inputFile), MetadataFileName);
        }

        private static bool IsInputFile(string path)
        {
            return Path.GetFileName(path).Equals(InputFileName, StringComparison.OrdinalIgnoreCase);
        }

        private static TranscoderTestMetadata GetMetadata(string inputFile)
        {
            string metadataFile = GetMetadataFile(inputFile);
            return JsonSerializer.Deserialize<TranscoderTestMetadata>(File.ReadAllText(metadataFile));
        }

        public static IEnumerable<TranscoderTestData> GetTestDatas(string testFileFolder)
        {
            IList<TranscoderTestData> result = new List<TranscoderTestData>();
            foreach (string path in Directory.EnumerateFiles(testFileFolder, AllFiles, SearchOption.AllDirectories))
            {
                if (IsInputFile(path))
                {
                    TranscoderTestData testData = new TranscoderTestData()
                    {
                        InputDicomFile = path,
                        ExpectedOutputDicomFile = GetExpectedOutputFile(path),
                        MetaData = GetMetadata(path),
                    };
                    result.Add(testData);
                }
            }

            return result;
        }
    }
}
