// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    /// <summary>
    /// Tag status on operation .
    /// </summary>
    public enum TagOperationStatus
    {
        /// <summary>
        /// The tag is being processed on the operation.
        /// </summary>
        Processing = 0,

        /// <summary>
        /// The tag is paused on the operation.
        /// </summary>
        Paused = 1
    }
}
