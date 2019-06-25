// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents
{
    internal class QueryInstance
    {
        [JsonConstructor]
        public QueryInstance(string sopInstanceUID, IDictionary<string, object[]> indexedAttributes)
            : this(sopInstanceUID)
        {
            EnsureArg.IsNotNull(indexedAttributes, nameof(indexedAttributes));

            IndexedAttributes = indexedAttributes;
        }

        private QueryInstance(string sopInstanceUID)
        {
            EnsureArg.IsTrue(DicomIdentifierValidator.IdentifierRegex.IsMatch(sopInstanceUID), nameof(sopInstanceUID));

            SopInstanceUID = sopInstanceUID;
        }

        public string SopInstanceUID { get; }

        public IDictionary<string, object[]> IndexedAttributes { get; } = new Dictionary<string, object[]>();

        public override int GetHashCode()
        {
            // Note: We override Equals() and GetHashCode() so if this class is added to a HashSet
            // we only compare on SopInstanceUID.
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

        public static QueryInstance Create(DicomDataset dicomDataset, IEnumerable<DicomAttributeId> indexableAttributes)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(indexableAttributes, nameof(indexableAttributes));

            var sopInstanceUID = dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);
            var result = new QueryInstance(sopInstanceUID);

            foreach (DicomAttributeId attribute in indexableAttributes)
            {
                if (dicomDataset.TryGetValues(attribute, out object[] values))
                {
                    result.IndexedAttributes[attribute.AttributeId] = values;
                }
            }

            return result;
        }
    }
}
