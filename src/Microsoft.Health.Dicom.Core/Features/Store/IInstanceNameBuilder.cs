// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Store;

/// <summary>
/// Provide file name for Dicom instance when store.
/// </summary>
public interface IInstanceNameBuilder
{
    /// <summary>
    /// Gets file name for Dicom instance.
    /// </summary>
    /// <param name="instanceIdentifier">The Dicom instance identifier.</param>
    /// <returns>The file name.</returns>
    string GetInstanceFileName(VersionedInstanceIdentifier instanceIdentifier);

    /// <summary>
    /// Gets file name for Dicom instance metadata.
    /// </summary>
    /// <param name="instanceIdentifier">The Dicom instance identifier.</param>
    /// <returns>The file name.</returns>
    string GetInstanceMetadataFileName(VersionedInstanceIdentifier instanceIdentifier);
}
