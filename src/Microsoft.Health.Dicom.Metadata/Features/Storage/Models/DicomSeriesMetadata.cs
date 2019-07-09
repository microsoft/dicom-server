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
        [JsonConstructor]
        public DicomSeriesMetadata(int currentInstanceId, IDictionary<string, int> instances, HashSet<AttributeValue> attributeValues)
        {
            EnsureArg.IsTrue(currentInstanceId >= 0, nameof(currentInstanceId));
            EnsureArg.IsNotNull(instances, nameof(instances));
            EnsureArg.IsNotNull(attributeValues, nameof(attributeValues));

            CurrentInstanceId = currentInstanceId;
            Instances = instances;
            AttributeValues = attributeValues;
        }

        [JsonProperty("currentInstanceId")]
        public int CurrentInstanceId { get; set; }

        [JsonProperty("instances")]
        public IDictionary<string, int> Instances { get; }

        [JsonProperty("values")]
        public HashSet<AttributeValue> AttributeValues { get; }
    }
}
