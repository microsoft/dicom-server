// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Client.Models;

public static class DefaultPartition
{
    public const string Name = "Microsoft.Default";
    public const int Key = 1;
    public static readonly Partition Partition = new Partition(Key, Name);
}