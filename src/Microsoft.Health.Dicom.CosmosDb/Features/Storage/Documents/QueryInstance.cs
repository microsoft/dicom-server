// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents
{
    internal class QueryInstance
    {
        [JsonConstructor]
        public QueryInstance(string sopInstanceUID, IList<(DicomTag, object)> indexedAttributes)
            : this(sopInstanceUID)
        {
            IndexedAttributes = indexedAttributes;
        }

        private QueryInstance(string sopInstanceUID)
        {
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUID, nameof(sopInstanceUID));
            SopInstanceUID = sopInstanceUID;
        }

        public string SopInstanceUID { get; }

        public IList<(DicomTag, object)> IndexedAttributes { get; } = new List<(DicomTag, object)>();

        public override int GetHashCode()
        {
            return SopInstanceUID.GetHashCode(StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is QueryInstance instance))
            {
                return false;
            }

            return SopInstanceUID.Equals(instance.SopInstanceUID, StringComparison.Ordinal);
        }

        public static QueryInstance Create(DicomDataset dicomItems, IEnumerable<DicomTag> indexTags)
        {
            var sopInstanceUID = dicomItems.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            var result = new QueryInstance(sopInstanceUID);

            foreach (DicomTag tag in indexTags)
            {
                if (dicomItems.TryGetString(tag, out string value))
                {
                    result.IndexedAttributes.Add((tag, value));
                }
            }

            return result;
        }
    }
}
