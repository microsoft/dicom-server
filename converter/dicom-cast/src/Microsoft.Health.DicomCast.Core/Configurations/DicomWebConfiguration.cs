// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.DicomCast.Core.Configurations;

/// <summary>
/// The configuration related to DICOMWeb service.
/// </summary>
public class DicomWebConfiguration
{
    /// <summary>
    /// The endpoint to DICOMWeb service.
    /// </summary>
    public Uri Endpoint { get; set; }

    /// <summary>
    /// The private endpoint to use to talk to dicom (this url will not be used when generating links in fhir)
    /// </summary>
    public Uri PrivateEndpoint { get; set; }

    /// <summary>
    /// Authentication settings for DICOMWeb.
    /// </summary>
    public AuthenticationConfiguration Authentication { get; set; }
}
