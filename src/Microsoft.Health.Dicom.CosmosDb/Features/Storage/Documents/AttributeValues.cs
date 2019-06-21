// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents
{
    public class AttributeValues
    {
        public AttributeValues()
        {
        }

        [JsonConstructor]
        public AttributeValues(HashSet<object> values)
        {
            EnsureArg.IsNotNull(values, nameof(values));

            Values = values;
        }

        /// <summary>
        /// Gets the minimum date time value from the array of values.
        /// We pre-calculate this to support range queries on DateTime values.
        /// This value does not need to be serialized if the array does not contain any DateTime values.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? MinDateTimeValue => Values.Where(x => x is DateTime).Min(x => (DateTime?)x);

        /// <summary>
        /// Gets the maximum date time value from the array of values.
        /// We pre-calculate this to support range queries on DateTime values.
        /// This value does not need to be serialized if the array does not contain any DateTime values.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? MaxDateTimeValue => Values.Where(x => x is DateTime).Max(x => (DateTime?)x);

        public HashSet<object> Values { get; } = new HashSet<object>();

        public bool Add(object value) => value == null ? false : Values.Add(value);
    }
}
