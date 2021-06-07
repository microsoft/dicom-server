// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Models.Operations.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Core.Models.Operations
{
    internal class DurableOrchestrationInstanceStatus
    {
        [JsonProperty("Name")]
        [JsonConverter(typeof(OperationTypeConverter))]
        public OperationType Type { get; set; }

        public string InstanceId { get; set; }

        public DateTime CreatedTime { get; set; }

        public JToken Input { get; set; }

        public JToken Output { get; set; }

        [JsonConverter(typeof(OperationStatusConverter))]
        public OperationStatus RuntimeStatus { get; set; }

        public JToken CustomStatus { get; set; }
    }
}
