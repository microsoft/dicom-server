// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.Store;

/// <summary>
///  Represent warnings when validation.
/// </summary>
[Flags]
public enum ValidationWarnings
{
    /// <summary>
    /// No warnings.
    /// </summary>
    None = 0,

    /// <summary>
    /// One or more Dicom tags in DicomDataset has multiple values
    /// </summary>
    IndexedDicomTagHasMultipleValues = 1,

    /// <summary>
    /// Data Set does not match SOP Class.
    /// </summary>
    DatasetDoesNotMatchSOPClass = 2,

    /// <summary>
    /// StudyInstanceUID has whitespace at the end.
    /// </summary>
    StudyInstanceUIDWhitespacePadding = 3,
}
