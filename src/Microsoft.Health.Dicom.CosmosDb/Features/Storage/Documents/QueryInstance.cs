// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents
{
    internal class QueryInstance
    {
        [JsonConstructor]
        public QueryInstance(string sopInstanceUID, IDictionary<string, object> indexedAttributes)
            : this(sopInstanceUID)
        {
            EnsureArg.IsNotNull(indexedAttributes, nameof(indexedAttributes));

            IndexedAttributes = indexedAttributes;
        }

        private QueryInstance(string sopInstanceUID)
        {
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUID, nameof(sopInstanceUID));
            EnsureArg.IsTrue(Regex.IsMatch(sopInstanceUID, DicomIdentifierValidator.IdentifierRegex));

            SopInstanceUID = sopInstanceUID;
        }

        public string SopInstanceUID { get; }

        public IDictionary<string, object> IndexedAttributes { get; } = new Dictionary<string, object>();

        public override int GetHashCode()
        {
            return SopInstanceUID.GetHashCode(StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (obj is QueryInstance instance)
            {
                return SopInstanceUID.Equals(instance.SopInstanceUID, StringComparison.Ordinal);
            }

            return false;
        }

        public static QueryInstance Create(DicomDataset dicomDataset, IEnumerable<DicomTag> indexTags)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            var sopInstanceUID = dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);
            var result = new QueryInstance(sopInstanceUID);

            if (indexTags != null)
            {
                foreach (DicomTag dicomTag in indexTags)
                {
                    // All indexed tags must have a value multiplicty of 1.
                    EnsureArg.IsTrue(dicomTag.DictionaryEntry.ValueMultiplicity.Multiplicity == 1);

                    if (dicomDataset.TryGetSingleValue(dicomTag, out object value))
                    {
                        result.IndexedAttributes[DicomTagSerializer.Serialize(dicomTag)] = value;
                    }
                }
            }

            return result;
        }
    }
}
