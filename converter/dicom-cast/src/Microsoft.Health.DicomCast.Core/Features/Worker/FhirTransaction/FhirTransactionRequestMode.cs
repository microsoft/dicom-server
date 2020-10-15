// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Represents the request mode.
    /// </summary>
    public enum FhirTransactionRequestMode
    {
        /// <summary>
        /// The resource did not change; no request needed.
        /// </summary>
        None,

        /// <summary>
        /// The resource needs to be created.
        /// </summary>
        Create,

        /// <summary>
        /// The resource needs to be updated.
        /// </summary>
        Update,

        /// <summary>
        /// The resource needs to be deleted.
        /// </summary>
        Delete,
    }
}
