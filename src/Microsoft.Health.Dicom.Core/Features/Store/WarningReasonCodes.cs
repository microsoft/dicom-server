// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Store;

/// <summary>
/// If any of the warning codes are modified, please check they match the DICOM conformance statement.
/// </summary>
internal static class WarningReasonCodes
{

    /// <summary>
    /// Data Set does not match SOP Class
    /// </summary>
    /// <remarks>
    /// The Studies Store Transaction (Section 10.5) observed that the Data Set did not match the constraints of the SOP Class during storage of the instance.
    /// </remarks>
    public const ushort DatasetDoesNotMatchSOPClass = 45063;

}
