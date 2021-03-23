// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Status of extended query tag.
    /// </summary>
    public enum ExtendedQueryTagStatus
    {
        /// <summary>
        /// The extended query tag is being added.
        /// </summary>
        Adding = 0,

        /// <summary>
        /// The extended query tag has been added to system.
        /// </summary>
        Ready = 1,

        /// <summary>
        /// The extended query tag is being deleted.
        /// </summary>
        Deleting = 2,
    }
}
