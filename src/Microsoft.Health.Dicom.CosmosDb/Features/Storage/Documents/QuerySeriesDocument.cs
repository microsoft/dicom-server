// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.RegularExpressions;
using EnsureThat;
using Microsoft.Health.CosmosDb.Features.Storage;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents
{
    internal class QuerySeriesDocument
    {
        /// <summary>
        /// Lists the characters that are not valid for a Cosmos resource identifier.
        /// https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.documents.resource.id?view=azure-dotnet
        /// </summary>
        private const string DocumentIdRegex = "[^?/\\#]";

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

        public Dictionary<string, AttributeValues> DistinctIndexedAttributes
        {
            get
            {
                var result = new Dictionary<string, AttributeValues>();

                foreach (QueryInstance instance in Instances)
                {
                    foreach ((string key, object value) in instance.IndexedAttributes)
                    {
                        if (!result.ContainsKey(key))
                        {
                            result[key] = new AttributeValues();
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

            EnsureArg.IsTrue(Regex.IsMatch(studyInstanceUID, DicomIdentifierValidator.IdentifierRegex));
            EnsureArg.IsTrue(Regex.IsMatch(seriesInstanceUID, DicomIdentifierValidator.IdentifierRegex));

            string documentId = studyInstanceUID + seriesInstanceUID;

            // Double safety for the document identifier. If the study and series are valid identifiers, the document ID 'should' conform.
            EnsureArg.IsTrue(Regex.IsMatch(documentId, DocumentIdRegex));

            return documentId;
        }

        public static string GetPartitionKey(string studyInstanceUID)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID, nameof(studyInstanceUID));
            EnsureArg.IsTrue(Regex.IsMatch(studyInstanceUID, DicomIdentifierValidator.IdentifierRegex));

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
