// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Client.Models;

/// <summary>
/// Used to ensure API contract does not change while we change the underlying property naming.
/// </summary>
public class PartitionEntry : Partition
{
    public PartitionEntry(int key, string name, DateTimeOffset createdDate = default) : base(key, name, createdDate)
    {
    }
}