// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to build the response for the store transaction.
    /// </summary>
    public interface IStoreResponseBuilder
    {
        /// <summary>
        /// Builds the response.
        /// </summary>
        /// <param name="studyInstanceUid">If specified and there is at least one success, then the RetrieveURL for the study will be set.</param>
        /// <returns>An instance of <see cref="StoreResponse"/> representing the response.</returns>
        StoreResponse BuildResponse(string studyInstanceUid);

        /// <summary>
        /// Adds a successful entry to the response.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset that was successfully stored.</param>
        void AddSuccess(DicomDataset dicomDataset);

        /// <summary>
        /// Adds a failed entry to the response.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset that failed to be stored.</param>
        /// <param name="failureReasonCode">The failure reason code.</param>
        void AddFailure(DicomDataset dicomDataset = null, ushort failureReasonCode = FailureReasonCodes.ProcessingFailure);
    }
}
