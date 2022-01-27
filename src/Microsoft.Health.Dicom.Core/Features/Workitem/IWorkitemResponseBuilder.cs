// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to build the response for the add workitem transaction.
    /// </summary>
    public interface IWorkitemResponseBuilder
    {
        /// <summary>
        /// Builds the response.
        /// </summary>
        /// <returns>An instance of <see cref="AddWorkitemResponse"/> representing the response.</returns>
        AddWorkitemResponse BuildAddResponse();

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
