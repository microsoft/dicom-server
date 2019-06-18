// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using EnsureThat;
using Microsoft.Health.CosmosDb.Features.Storage;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents
{
    internal class QuerySeriesDocument
    {
        public QuerySeriesDocument(string studyInstanceUID, string seriesInstanceUID)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID, nameof(studyInstanceUID));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUID, nameof(seriesInstanceUID));
            EnsureArg.IsFalse(studyInstanceUID == seriesInstanceUID, nameof(seriesInstanceUID));

            StudyInstanceUID = studyInstanceUID;
            SeriesInstanceUID = seriesInstanceUID;
            Id = GetDocumentId(studyInstanceUID, seriesInstanceUID);
            PartitionKey = GetPartitionKey(studyInstanceUID);
        }

        [JsonProperty(KnownDocumentProperties.Id)]
        public string Id { get; }

        [JsonProperty(KnownDocumentProperties.PartitionKey)]
        public string PartitionKey { get; }

        [JsonProperty(KnownDocumentProperties.ETag)]
        public string ETag { get; set; }

        public string StudyInstanceUID { get; }

        public string SeriesInstanceUID { get; }

        public HashSet<QueryInstance> Instances { get; } = new HashSet<QueryInstance>();

        public Dictionary<DicomTag, HashSet<object>> DistinctIndexedAttributes
        {
            get
            {
                var result = new Dictionary<DicomTag, HashSet<object>>();

                foreach (QueryInstance instance in Instances)
                {
                    foreach ((DicomTag key, object value) in instance.IndexedAttributes)
                    {
                        if (!result.ContainsKey(key))
                        {
                            result[key] = new HashSet<object>();
                        }

                        result[key].Add(value);
                    }
                }

                return result;
            }
        }

        public static string GetDocumentId(string studyInstanceUID, string seriesInstanceUID)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID, nameof(studyInstanceUID));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUID, nameof(seriesInstanceUID));
            EnsureArg.IsFalse(studyInstanceUID == seriesInstanceUID);
            return studyInstanceUID + seriesInstanceUID;
        }

        public static string GetPartitionKey(string studyInstanceUID)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID, nameof(studyInstanceUID));
            return studyInstanceUID;
        }

        public bool AddInstance(QueryInstance instance)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            return Instances.Add(instance);
        }

        public bool RemoveInstance(string sopInstanceUID)
        {
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUID, nameof(sopInstanceUID));
            return Instances.RemoveWhere(x => x.SopInstanceUID == sopInstanceUID) > 0;
        }
    }
}
