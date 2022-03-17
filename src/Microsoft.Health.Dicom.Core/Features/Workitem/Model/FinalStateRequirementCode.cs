// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Workitem.Model;

/// <summary>
/// CC.2.5.1.1 UPS Final State Requirements
/// <see href="https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.5-1">Table CC.2.5-1. Final State Codes</see>
/// </summary>
public enum FinalStateRequirementCode
{
    /// <summary>
    /// The UPS State may be set to either COMPLETED or CANCELED if this Attribute does not have a value.
    /// </summary>
    O,

    /// <summary>
    /// The UPS State shall not be set to COMPLETED or CANCELED if this Attribute does not have a value.
    /// </summary>
    R,

    /// <summary>
    /// The UPS State shall not be set to COMPLETED or CANCELED if the condition is met and this Attribute does not have a value.
    /// </summary>
    RC,

    /// <summary>
    /// The UPS State shall not be set to COMPLETED if this Attribute does not have a value, but may be set to CANCELED.
    /// </summary>
    P,

    /// <summary>
    /// The UPS State shall not be set to CANCELED if this Attribute does not have a value, but may be set to COMPLETED.
    /// </summary>
    X,
}
