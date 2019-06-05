// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dicom;
using Dicom.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Web.Tests.E2E
{
    public static class DicomSamples
    {
        public static IReadOnlyCollection<DicomFile> GetSampleCTSeries()
               => GetDicomFilesFromDirectory(@"TestCases\ProstateJson").ToList();

        private static IEnumerable<DicomFile> GetDicomFilesFromDirectory(string directory)
        {
            var mapping = new Dictionary<string, string>();

            foreach (var path in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                var jsonDicomFile = File.ReadAllText(path);
                var serializationSettings = new JsonSerializerSettings();
                serializationSettings.Converters.Add(new JsonDicomConverter(writeTagsAsKeywords: true));

                DicomDataset dicomDataset = JsonConvert.DeserializeObject<DicomDataset>(jsonDicomFile, serializationSettings);

                // Consistently randomise the study/ series/ instance identifiers.
                AddOrUpdateIdentifierIfNotNull(dicomDataset, DicomTag.StudyInstanceUID, mapping);
                AddOrUpdateIdentifierIfNotNull(dicomDataset, DicomTag.SeriesInstanceUID, mapping);
                AddOrUpdateIdentifierIfNotNull(dicomDataset, DicomTag.SOPInstanceUID, mapping);

                // Set valid study date times/ patient birth dates.
                dicomDataset.AddOrUpdate(new DicomDateTime(DicomTag.StudyDate, DateTime.Today.AddDays(-1)));
                dicomDataset.AddOrUpdate(new DicomDateTime(DicomTag.PatientBirthDate, DateTime.Today.AddYears(-29)));

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
