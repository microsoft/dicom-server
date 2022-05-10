// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Configs;

/// <summary>
/// Type of the blob format
/// </summary>
public enum BlobMigrationFormatType
{
    // Old format with Uids `DicomFileNameWithUid`
    Old = 0,

    // Support writing blob with old uid format and new format
    Dual = 1,

    // Support writing blob with new format `DicomFileNameWithPrefix`
    New = 2
}
