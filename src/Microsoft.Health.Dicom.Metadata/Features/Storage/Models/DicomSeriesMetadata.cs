// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage.Models
{
    internal class DicomSeriesMetadata
    {
        [JsonProperty("instanceIdentiferMap")]
        private readonly IDictionary<string, int> _instanceIdentiferMap;

        [JsonProperty("currentInstanceId")]
        private int _currentInstanceId;

        [JsonConstructor]
        public DicomSeriesMetadata(int currentInstanceId, IDictionary<string, int> instanceIdentiferMap, HashSet<DicomItemInstances> dicomItems)
        {
            EnsureArg.IsTrue(currentInstanceId >= 0, nameof(currentInstanceId));
            EnsureArg.IsNotNull(instanceIdentiferMap, nameof(instanceIdentiferMap));
            EnsureArg.IsNotNull(dicomItems, nameof(dicomItems));

            _currentInstanceId = currentInstanceId;
            _instanceIdentiferMap = instanceIdentiferMap;
            DicomItems = dicomItems;
        }

        [JsonIgnore]
        public IReadOnlyCollection<string> Instances => _instanceIdentiferMap.Keys as IReadOnlyCollection<string>;

        [JsonProperty("values")]
        public HashSet<DicomItemInstances> DicomItems { get; }

        public int CreateOrGetInstanceIdentifier(string sopInstanceUID)
        {
            if (!_instanceIdentiferMap.TryGetValue(sopInstanceUID, out int instanceId))
            {
                instanceId = _currentInstanceId++;
                _instanceIdentiferMap.Add(sopInstanceUID, instanceId);
            }

            return instanceId;
        }

        public void RemoveInstance(string sopInstanceUID)
        {
            _instanceIdentiferMap.Remove(sopInstanceUID);
        }
    }
}
