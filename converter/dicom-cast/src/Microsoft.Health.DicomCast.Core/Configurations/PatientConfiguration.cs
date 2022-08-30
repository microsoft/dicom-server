// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DicomCast.Core.Configurations;

/// <summary>
/// Configuration for Patient system Id
/// </summary>
public class PatientConfiguration
{
    /// <summary>
    /// Patient System Id configured by the user
    /// </summary>
    public string PatientSystemId { get; set; }

    /// <summary>
    /// Issuer Id or Patient System Id used based on this boolean value
    /// </summary>
    public bool IsIssuerIdUsed { get; set; }
}
