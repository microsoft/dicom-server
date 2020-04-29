// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Messages.Store
{
    /// <summary>
    /// Represents the store transaction response status.
    /// </summary>
    public enum DicomStoreResponseStatus
    {
        /// <summary>
        /// There is no DICOM instance to store.
        /// </summary>
        None,

        /// <summary>
        /// All DICOM instance(s) have been stored successfully.
        /// </summary>
        Success,

        /// <summary>
        /// Some DICOM instance(s) have been stored successfully.
        /// </summary>
        PartialSuccess,

        /// <summary>
        /// All DICOM instance(s) have failed to be stored.
        /// </summary>
        Failure,
    }
}
