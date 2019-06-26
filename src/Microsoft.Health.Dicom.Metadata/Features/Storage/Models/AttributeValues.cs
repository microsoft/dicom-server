// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage.Models
{
    /// <summary>
    /// Class for keeping a collection of the Values for this tag and the instances that
    /// created the value.
    /// </summary>
    internal class AttributeValues
    {
        [JsonConstructor]
        public AttributeValues(IDictionary<object, HashSet<int>> values)
        {
            Values = values;
        }

        public IDictionary<object, HashSet<int>> Values { get; }

        public static AttributeValues Create(int instanceId, object value)
        {
            EnsureArg.IsTrue(instanceId >= 0, nameof(instanceId));
            EnsureArg.IsNotNull(value, nameof(value));

            return new AttributeValues(new Dictionary<object, HashSet<int>>() { { value, new HashSet<int>() { instanceId } } });
        }

        public void Add(int instanceId, object value)
        {
            if (Values.ContainsKey(value))
            {
                Values[value].Add(instanceId);
            }
            else
            {
                Values[value] = new HashSet<int>() { instanceId };
            }
        }

        public void Remove(int instanceId)
        {
            EnsureArg.IsTrue(instanceId >= 0, nameof(instanceId));

            var keysToRemove = new List<object>();

            // Remove the SOPInstance from every value this instance matched, keep a track of the values
            // that become empty after removing.
            foreach (KeyValuePair<object, HashSet<int>> value in Values)
            {
                if (value.Value.Remove(instanceId) && value.Value.Count == 0)
                {
                    keysToRemove.Add(value.Key);
                }
            }

            // Remove the empty values.
            foreach (object value in keysToRemove)
            {
                Values.Remove(value);
            }
        }
    }
}
