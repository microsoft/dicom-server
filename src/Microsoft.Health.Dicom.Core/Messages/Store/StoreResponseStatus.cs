// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Messages.Store;

/// <summary>
/// Represents the store transaction response status.
/// </summary>
public enum StoreResponseStatus
{
    /// <summary>
    /// There is no DICOM instance to store.
    /// </summary>
    None,

    /// <summary>
    /// The origin server successfully stored all Instances.
    /// </summary>
    Success,

    /// <summary>
    /// The origin server stored some of the Instances and failures exist for others.
    /// Or origin server stored has warnings for Instances stored.
    /// Additional information regarding this error may be found in the response message body.
    /// </summary>
    PartialSuccess,

    /// <summary>
    /// The origin server was unable to store any instances due to bad syntax.
    /// The request was formed correctly but the origin server was unable to store any instances due to a conflict
    /// in the request (e.g., unsupported SOP Class or Study Instance UID mismatch).
    /// This may also be used to indicate that the origin server was unable to store any instances for a mixture of reasons.
    /// Additional information regarding the instance errors may be found in the payload.
    /// </summary>
    Failure,
}
