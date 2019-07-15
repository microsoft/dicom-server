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
        [JsonProperty("currentInstanceId")]
        private int _currentInstanceId;

        [JsonConstructor]
        public DicomSeriesMetadata(int currentInstanceId, IDictionary<string, int> instances, HashSet<DicomItemInstances> dicomItems)
        {
            EnsureArg.IsTrue(currentInstanceId >= 0, nameof(currentInstanceId));
            EnsureArg.IsNotNull(instances, nameof(instances));
            EnsureArg.IsNotNull(dicomItems, nameof(dicomItems));

            _currentInstanceId = currentInstanceId;
            Instances = instances;
            DicomItems = dicomItems;
        }

        [JsonProperty("instances")]
        public IDictionary<string, int> Instances { get; }

        [JsonProperty("values")]
        public HashSet<DicomItemInstances> DicomItems { get; }

        public int AddSopInstanceUID(string sopInstanceUID)
        {
            var instanceId = _currentInstanceId++;
            Instances.Add(sopInstanceUID, instanceId);
            return instanceId;
        }
    }
}
