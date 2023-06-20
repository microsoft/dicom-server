// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Client.Models;

public class Partition
{
    public const string DefaultName = "Microsoft.Default";

    public const int DefaultKey = 1;

    public int Key { get; }

    public string Name { get; }

    public static Partition Default => new(DefaultKey, DefaultName);

    public Partition(int key, string name)
    {
        Key = key;
        Name = EnsureArg.IsNotNull(name, nameof(name));
    }
}