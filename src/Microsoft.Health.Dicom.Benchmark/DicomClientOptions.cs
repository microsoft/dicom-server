// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Dicom.Benchmark;

[SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "This class is deserialized.")]
internal sealed class DicomClientOptions
{
    public Uri BaseAddress { get; set; }
}
