// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Messages.WorkItemMessages
{
    /// <summary>
    /// Represents the store work-item response status.
    /// </summary>
    public enum WorkItemStoreResponseStatus
    {
        /// <summary>
        /// There is no DICOM instance to store.
        /// </summary>
        None,

        /// <summary>
        /// All DICOM work-item instance(s) have been stored successfully.
        /// </summary>
        Success,

        /// <summary>
        /// All DICOM work-item instance(s) have failed to be stored.
        /// </summary>
        Failure,
    }
}
