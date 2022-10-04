// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Running;
using Microsoft.Health.Dicom.Benchmark.Retrieve;

namespace Microsoft.Health.Dicom.Benchmark;

public static class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<WadoBenchmark>();
    }
}
