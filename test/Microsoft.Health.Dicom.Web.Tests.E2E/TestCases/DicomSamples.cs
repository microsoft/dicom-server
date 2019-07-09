// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dicom;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Web.Tests.E2E
{
    public static class DicomSamples
    {
        public static IReadOnlyCollection<DicomFile> GetSampleCTSeries(bool randomiseUIDs = true)
               => GetDicomFilesFromDirectory(@"TestCases\ProstateJson", randomiseUIDs).ToList();

        private static IEnumerable<DicomFile> GetDicomFilesFromDirectory(string directory, bool randomiseUIDs)
        {
            var randomMapping = new Dictionary<string, string>();

            foreach (var path in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                var jsonDicomFile = File.ReadAllText(path);
                var serializationSettings = new JsonSerializerSettings();

                DicomDataset dicomDataset = JsonConvert.DeserializeObject<DicomDataset>(jsonDicomFile, serializationSettings);

                // Consistently randomise the study/ series/ instance identifiers.
                if (randomiseUIDs)
                {
                    AddOrUpdateIdentifierIfNotNull(dicomDataset, DicomTag.StudyInstanceUID, randomMapping);
                    AddOrUpdateIdentifierIfNotNull(dicomDataset, DicomTag.SeriesInstanceUID, randomMapping);
                    AddOrUpdateIdentifierIfNotNull(dicomDataset, DicomTag.SOPInstanceUID, randomMapping);
                }

                yield return new DicomFile(dicomDataset);
            }
        }

        private static void AddOrUpdateIdentifierIfNotNull(DicomDataset dicomDataset, DicomTag dicomTag, Dictionary<string, string> mapping)
        {
            var instanceIdentifier = dicomDataset.GetSingleValueOrDefault<string>(dicomTag, null);

            if (instanceIdentifier == null)
            {
                return;
            }

            if (!mapping.ContainsKey(instanceIdentifier))
            {
                mapping[instanceIdentifier] = Guid.NewGuid().ToString();
            }

            dicomDataset.AddOrUpdate(dicomTag, mapping[instanceIdentifier]);
        }
    }
}
