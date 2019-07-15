// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using Dicom.Serialization;
using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage.Models
{
    internal class DicomItemInstances
    {
        [JsonProperty("jsonItem")]
        private readonly string _jsonItem;
        private readonly StringComparison _stringComparison = StringComparison.InvariantCultureIgnoreCase;
        private readonly JsonDicomConverter _jsonDicomConverter = new JsonDicomConverter();

        public DicomItemInstances(DicomItem dicomItem, HashSet<int> instances)
        {
            DicomItem = EnsureArg.IsNotNull(dicomItem, nameof(dicomItem));
            Instances = EnsureArg.IsNotNull(instances, nameof(instances));

            _jsonItem = SerializeDicomItem(dicomItem);
        }

        [JsonConstructor]
        public DicomItemInstances(string jsonItem, HashSet<int> instances)
        {
            _jsonItem = EnsureArg.IsNotNullOrWhiteSpace(jsonItem, nameof(jsonItem));
            Instances = EnsureArg.IsNotNull(instances, nameof(instances));

            DicomItem = JsonConvert.DeserializeObject<DicomDataset>(_jsonItem, _jsonDicomConverter).First();
        }

        [JsonProperty("instances")]
        public HashSet<int> Instances { get; }

        [JsonIgnore]
        public DicomItem DicomItem { get; }

        public static DicomItemInstances Create(DicomItem dicomItem, int instance)
        {
            EnsureArg.IsGte(instance, 0, nameof(instance));
            EnsureArg.IsNotNull(dicomItem, nameof(dicomItem));

            return new DicomItemInstances(dicomItem, new HashSet<int>() { instance });
        }

        public override int GetHashCode()
            => _jsonItem.GetHashCode(_stringComparison);

        public override bool Equals(object obj)
        {
            if (obj is DicomItemInstances dicomItemInstances)
            {
                return dicomItemInstances._jsonItem.Equals(_jsonItem, _stringComparison);
            }

            return false;
        }

        private string SerializeDicomItem(DicomItem dicomItem)
            => JsonConvert.SerializeObject(new DicomDataset(dicomItem), _jsonDicomConverter);
    }
}
