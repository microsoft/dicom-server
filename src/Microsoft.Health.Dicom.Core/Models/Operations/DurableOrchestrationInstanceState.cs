// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Core.Model.Operations
{
    public class DurableOrchestrationInstanceState
    {
        public string Name { get; set; }

        public string InstanceId { get; set; }

        public DateTime CreatedTime { get; set; }

        public JToken Input { get; set; }

        public JToken Output { get; set; }

        public string RuntimeStatus { get; set; }

        public JToken CustomStatus { get; set; }
    }
}
