// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Models
{
    [DataContract]
    public class AnonymizerConfiguration
    {
        [DataMember(Name = "rules")]
        public JObject[] RuleContent { get; set; }

        [DataMember(Name = "defaultSettings")]
        public AnonymizerDefaultSettings DefaultSettings { get; set; }

        [DataMember(Name = "customSettings")]
        public Dictionary<string, JObject> CustomSettings { get; set; }
    }
}
