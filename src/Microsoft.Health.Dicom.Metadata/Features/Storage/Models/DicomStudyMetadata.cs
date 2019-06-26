// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage.Models
{
    internal class DicomStudyMetadata
    {
        public DicomStudyMetadata(string studyInstanceUID)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID, nameof(studyInstanceUID));

            StudyInstanceUID = studyInstanceUID;
            SeriesMetadata = new Dictionary<string, DicomSeriesMetadata>();
        }

        [JsonConstructor]
        public DicomStudyMetadata(string studyInstanceUID, IDictionary<string, DicomSeriesMetadata> seriesMetadata)
            : this(studyInstanceUID)
        {
            EnsureArg.IsNotNull(seriesMetadata, nameof(seriesMetadata));

            SeriesMetadata = seriesMetadata;
        }

        public string StudyInstanceUID { get; }

        public IDictionary<string, DicomSeriesMetadata> SeriesMetadata { get; }

        public void AddInstance(DicomDataset instance, IEnumerable<DicomAttributeId> indexableAttributes)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(indexableAttributes, nameof(indexableAttributes));

            // If this series instance does not exist, we need to create a new dictionary entry.
            var identity = DicomInstance.Create(instance);
            if (!SeriesMetadata.TryGetValue(identity.SeriesInstanceUID, out DicomSeriesMetadata seriesMetadata))
            {
                seriesMetadata = new DicomSeriesMetadata();
                SeriesMetadata[identity.SeriesInstanceUID] = seriesMetadata;
            }

            seriesMetadata.AddInstance(instance, indexableAttributes);
        }

        public IEnumerable<DicomInstance> GetDicomInstances()
        {
            foreach (KeyValuePair<string, DicomSeriesMetadata> series in SeriesMetadata)
            {
                foreach (KeyValuePair<string, int> instance in series.Value.SopInstances)
                {
                    yield return new DicomInstance(StudyInstanceUID, series.Key, instance.Key);
                }
            }
        }

        public IEnumerable<DicomInstance> GetDicomInstances(string seriesInstanceUID)
        {
            if (SeriesMetadata.TryGetValue(seriesInstanceUID, out DicomSeriesMetadata seriesMetadata))
            {
                return seriesMetadata.SopInstances.Select(x => new DicomInstance(StudyInstanceUID, seriesInstanceUID, x.Key));
            }

            throw new ArgumentException($"The provided series instance identifier does not exist: {seriesInstanceUID}", nameof(seriesInstanceUID));
        }
    }
}
