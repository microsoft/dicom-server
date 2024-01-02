// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.Options;

namespace Microsoft.Health.Dicom.Functions.Client;

internal class DicomFunctionOptions
{
    public const string SectionName = "DicomFunctions";

    [Required]
    public FanOutFunctionOptions ContentLengthBackFill { get; set; }

    [Required]
    public FanOutFunctionOptions DataCleanup { get; set; }

    [Required]
    public DurableClientOptions DurableTask { get; set; }

    [Required]
    public FanOutFunctionOptions Export { get; set; }

    [Required]
    public FanOutFunctionOptions Indexing { get; set; }

    [Required]
    public FanOutFunctionOptions Update { get; set; }
}
