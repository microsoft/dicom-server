// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.SqlServer.Features.Schema
{
    /// <summary>
    ///  Support versioning.
    /// </summary>
    internal interface IVersioned
    {
        /// <summary>
        /// Get the Schema version
        /// </summary>
        SchemaVersion Version { get; }
    }
}
