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
    internal class QuerySeriesDocument : IDocument
    {
        /// <summary>
        /// The seperator between the Study and Series instance identifiers when creating the document identifier.
        /// </summary>
        private const char DocumentIdSeperator = '_';

        /// <summary>
        /// Lists the characters that are not valid for a Cosmos resource identifier.
        /// https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.documents.resource.id?view=azure-dotnet
        /// </summary>
        private static readonly Regex DocumentIdRegex = new Regex("[^?/\\#]", RegexOptions.Singleline | RegexOptions.Compiled);

        public QuerySeriesDocument(string studyUID, string seriesUID)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyUID, nameof(studyUID));
            EnsureArg.IsNotNullOrWhiteSpace(seriesUID, nameof(seriesUID));
            EnsureArg.IsFalse(studyUID == seriesUID, nameof(seriesUID));

            StudyUID = studyUID;
            SeriesUID = seriesUID;
            Id = GetDocumentId(studyUID, seriesUID);
            PartitionKey = GetPartitionKey(studyUID);
        }

        [JsonProperty(KnownDocumentProperties.Id)]
        public string Id { get; }

        [JsonProperty(KnownDocumentProperties.PartitionKey)]
        public string PartitionKey { get; }

        [JsonProperty(KnownDocumentProperties.ETag)]
        public string ETag { get; set; }

        [JsonProperty(DocumentProperties.StudyInstanceUID)]
        public string StudyUID { get; }

        [JsonProperty(DocumentProperties.SeriesInstanceUID)]
        public string SeriesUID { get; }

        [JsonProperty(DocumentProperties.Instances)]
        public HashSet<QueryInstance> Instances { get; } = new HashSet<QueryInstance>();

        [JsonProperty(DocumentProperties.DistinctAttributes)]
        public IDictionary<string, AttributeValues> DistinctAttributes
        {
            get
            {
                var result = new Dictionary<string, AttributeValues>();

                foreach (QueryInstance instance in Instances)
                {
                    foreach ((string key, object[] values) in instance.Attributes)
                    {
                        if (!result.ContainsKey(key))
                        {
                            result[key] = new AttributeValues();
                        }

                        foreach (object value in values)
                        {
                            result[key].Add(value);
                        }
                    }
                }

                return result;
            }
        }

        public static string GetDocumentId(string studyUID, string seriesUID)
        {
            EnsureArg.IsNotEqualTo(seriesUID, studyUID, nameof(seriesUID));
            EnsureArg.Matches(studyUID, DicomIdentifierValidator.IdentifierRegex, nameof(studyUID));
            EnsureArg.Matches(seriesUID, DicomIdentifierValidator.IdentifierRegex, nameof(seriesUID));

            string documentId = $"{studyUID}{DocumentIdSeperator}{seriesUID}";

            // Double safety for the document identifier. If the study and series are valid identifiers, the document ID 'should' conform.
            EnsureArg.Matches(documentId, DocumentIdRegex, nameof(documentId));

            return documentId;
        }

        public static string GetPartitionKey(string studyUID)
        {
            EnsureArg.Matches(studyUID, DicomIdentifierValidator.IdentifierRegex, nameof(studyUID));

            return studyUID;
        }

        public bool AddInstance(QueryInstance instance)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            return Instances.Add(instance);
        }

        public bool RemoveInstance(string sopInstanceUID)
        {
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUID, nameof(sopInstanceUID));
            return Instances.RemoveWhere(x => x.InstanceUID == sopInstanceUID) > 0;
        }
    }
}
