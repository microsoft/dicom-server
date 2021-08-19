// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Functions.Client.Configs
{
    public class FunctionsClientOptions
    {
        internal const string SectionName = "DicomFunctions";

        [Required]
        public Uri BaseAddress { get; set; }

        [Required]
        public OperationRoutes Routes { get; set; } = new OperationRoutes();

        [Range(0, int.MaxValue)]
        public int MaxRetries { get; set; }

        [Range(0, 10000)]
        public int MinRetryDelayMilliseconds { get; set; }

        public string FunctionAccessKey { get; set; }
    }
}
