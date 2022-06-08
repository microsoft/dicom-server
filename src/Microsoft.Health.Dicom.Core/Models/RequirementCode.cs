// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Models;

/// <summary>
/// Service class user (SCU) and service class provider (SCP) requirements.
/// <see href="https://dicom.nema.org/medical/dicom/current/output/html/part04.html#sect_5.4.2.1">Dicom 3.4.5.4.2.1</see>
/// </summary>
public enum RequirementCode
{
    /// <summary>
    /// Mandatory for the SCU, and cannot be zero length.
    /// </summary>
    OneOne,
    /// <summary>
    /// Mandatory for the SCU, and can be zero length. The SCP will apply a default.
    /// </summary>
    TwoOne,
    /// <summary>
    /// Mandatory for the SCU, and can be zero length.
    /// </summary>
    TwoTwo,
    /// <summary>
    /// Optional for the SCU, and cannot be zero length.
    /// </summary>
    ThreeOne,
    /// <summary>
    /// Optional for the SCU, and cannot be zero length.
    /// </summary>
    ThreeTwo,
    /// <summary>
    /// Optional for the SCU, and cannot be zero length.
    /// </summary>
    ThreeThree,
}
