// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Core.Configs
{
    public class FunctionsClientConfiguration
    {
        public const string SectionName = "Functions";

        [Required]
        public Uri BaseAddress { get; set; }

        [Required]
        public OperationRoutesConfiguration Routes { get; set; } = new OperationRoutesConfiguration();

        [Range(0, int.MaxValue)]
        public int MaxRetries { get; set; }

        [Range(0, 10000)]
        public int MinRetryDelayMilliseconds { get; set; }
    }
}
