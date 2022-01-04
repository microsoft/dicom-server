// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to build the response for the workitem store transaction.
    /// </summary>
    public interface IWorkitemStoreResponseBuilder
    {
        /// <summary>
        /// Builds the response.
        /// </summary>
        /// <param name="workitemInstanceUid">If specified and there is at least one success, then the RetrieveURL for the study will be set.</param>
        /// <returns>An instance of <see cref="WorkitemStoreResponse"/> representing the response.</returns>
        WorkitemStoreResponse BuildResponse(string workitemInstanceUid);

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
