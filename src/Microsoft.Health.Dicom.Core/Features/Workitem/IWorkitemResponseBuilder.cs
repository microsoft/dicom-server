// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Messages.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

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
    /// Builds the response for cancel workitem.
    /// </summary>
    /// <returns>An instance of <see cref="CancelWorkitemResponse"/> representing the response.</returns>
    CancelWorkitemResponse BuildCancelResponse();

    /// <summary>
    /// Builds the response for retrieve workitem
    /// </summary>
    /// <returns>An instance of <see cref="RetrieveWorkitemResponse"/> representing the response.</returns>
    RetrieveWorkitemResponse BuildRetrieveWorkitemResponse();

    /// <summary>
    /// Builds the response for update workitem.
    /// </summary>
    /// <returns>An instance of <see cref="UpdateWorkitemResponse"/> represeting the response.</returns>
    UpdateWorkitemResponse BuildUpdateWorkitemResponse();

    /// <summary>
    /// Adds a successful entry to the response.
    /// </summary>
    /// <param name="dicomDataset">The DICOM dataset that was successfully stored.</param>
    void AddSuccess(DicomDataset dicomDataset);

    /// <summary>
    /// Adds a successful entry to the response with a status message
    /// </summary>
    /// <param name="message">The message related to the status</param>
    void AddSuccess(string message);

    /// <summary>
    /// Adds a failed entry to the response.
    /// </summary>
    /// <param name="failureReasonCode">The failure reason code.</param>
    /// <param name="message">The message related to the failure</param>
    /// <param name="dicomDataset">The DICOM dataset that failed to be stored.</param>
    void AddFailure(ushort? failureReasonCode, string message = null, DicomDataset dicomDataset = null);
}
