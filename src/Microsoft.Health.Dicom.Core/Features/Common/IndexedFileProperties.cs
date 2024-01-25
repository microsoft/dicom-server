// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Common;

/// <summary>
/// Metadata on FileProperty table in database
/// </summary>
public class IndexedFileProperties
{
    /// <summary>
    /// Total indexed FileProperty in database
    /// </summary>
    public int TotalIndexed { get; init; }

    /// <summary>
    /// Total sum of all ContentLength rows in FileProperty table
    /// </summary>
    public long TotalSum { get; init; }
}
