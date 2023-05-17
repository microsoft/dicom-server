// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Core.Features.Model;

/// <summary>
/// Represents file properties for an instance.
/// </summary>
public class InstanceFileProperties
{
    /// <summary>
    /// File properties of instance
    /// </summary>
    public FileProperties FileProperties { get; init; }

    /// <summary>
    /// InstanceKey of instance
    /// </summary>
    public long? InstanceKey { get; init; }
}