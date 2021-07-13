// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Feature.Common
{
    /// <summary>
    /// Represents versioned functionality.
    /// </summary>
    internal interface IVersioned
    {
        /// <summary>
        /// Gets the range of supported versions.
        /// </summary>
        VersionRange SupportedVersions { get; }
    }
}
