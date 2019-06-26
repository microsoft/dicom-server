// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage.Models
{
    internal class DicomSeriesMetadata
    {
        public DicomSeriesMetadata()
        {
            InstanceIdCount = 1;
            SopInstances = new Dictionary<string, int>();
            IndexedAttributes = new Dictionary<string, AttributeValues>();
        }

        [JsonConstructor]
        public DicomSeriesMetadata(
            int instanceIdCount,
            IDictionary<string, int> sopInstances,
            IDictionary<string, AttributeValues> indexedAttributes)
        {
            EnsureArg.IsTrue(instanceIdCount >= 0, nameof(instanceIdCount));
            EnsureArg.IsNotNull(sopInstances, nameof(sopInstances));
            EnsureArg.IsNotNull(indexedAttributes, nameof(indexedAttributes));

            InstanceIdCount = instanceIdCount;
            SopInstances = sopInstances;
            IndexedAttributes = indexedAttributes;
        }

        public int InstanceIdCount { get; private set; }

        public IDictionary<string, int> SopInstances { get; }

        public IDictionary<string, AttributeValues> IndexedAttributes { get; }

        public void AddInstance(DicomDataset dicomDataset, IEnumerable<DicomAttributeId> indexableAttributes)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            var sopInstanceUID = dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);
            EnsureArg.IsTrue(DicomIdentifierValidator.IdentifierRegex.IsMatch(sopInstanceUID));

            if (!SopInstances.ContainsKey(sopInstanceUID))
            {
                SopInstances[sopInstanceUID] = InstanceIdCount++;
            }

            var instanceId = SopInstances[sopInstanceUID];

            foreach (DicomAttributeId attribute in indexableAttributes)
            {
                if (dicomDataset.TryGetValues(attribute, out object[] values))
                {
                    foreach (object value in values)
                    {
                        if (IndexedAttributes.ContainsKey(attribute.AttributeId))
                        {
                            IndexedAttributes[attribute.AttributeId].Add(instanceId, value);
                        }
                        else
                        {
                            IndexedAttributes[attribute.AttributeId] = AttributeValues.Create(instanceId, value);
                        }
                    }
                }
            }
        }

        public void RemoveInstance(string sopInstanceUID)
        {
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUID, nameof(sopInstanceUID));

            if (!SopInstances.ContainsKey(sopInstanceUID))
            {
                return;
            }

            var instanceId = SopInstances[sopInstanceUID];

            foreach (KeyValuePair<string, AttributeValues> pair in IndexedAttributes)
            {
                pair.Value.Remove(instanceId);
            }
        }
    }
}
