// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Store;

/// <summary>
/// Provides functionality to build the response for the store transaction.
/// </summary>
public interface IStoreResponseBuilder
{
    /// <summary>
    /// Builds the response.
    /// </summary>
    /// <param name="studyInstanceUid">If specified and there is at least one success, then the RetrieveURL for the study will be set.</param>
    /// <param name="returnWarning202">Whether to return 202 when warning set or not</param>
    /// <returns>An instance of <see cref="StoreResponse"/> representing the response.</returns>
    StoreResponse BuildResponse(string studyInstanceUid, bool returnWarning202 = false);

    /// <summary>
    /// Adds a Success entry to the response.
    /// </summary>
    /// <param name="dicomDataset">The DICOM dataset that was successfully stored.</param>
    /// <param name="storeValidationResult">Store validation errors and warnings</param>
    /// <param name="partition">Data Partition entry</param>
    /// <param name="warningReasonCode">The warning reason code.</param>
    /// <param name="buildWarningSequence">Whether to build response warning sequence or not.</param>
    void AddSuccess(DicomDataset dicomDataset,
        StoreValidationResult storeValidationResult,
        Partition partition,
        ushort? warningReasonCode = null,
        bool buildWarningSequence = false);

    void SetWarningMessage(string message);

    /// <summary>
    /// Adds a failed entry to the response.
    /// </summary>
    /// <param name="dicomDataset">The DICOM dataset that failed to be stored.</param>
    /// <param name="failureReasonCode">The failure reason code.</param>
    /// <param name="storeValidationResult">Store validation errors and warnings</param>
    void AddFailure(
        DicomDataset dicomDataset = null,
        ushort failureReasonCode = FailureReasonCodes.ProcessingFailure,
        StoreValidationResult storeValidationResult = null);
}
