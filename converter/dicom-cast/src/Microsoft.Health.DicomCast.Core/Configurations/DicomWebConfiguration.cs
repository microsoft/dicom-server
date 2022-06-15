// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Client.Configuration;

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
    /// The optional private endpoint to use to talk to dicom
    /// </summary>
    /// <remarks>
    /// If this  url is specified then it will be used to talk to dicom, but it will not be used when specifying the url in the fhir objects.
    /// The value of <see cref="Endpoint" /> will still be used to generate links to dicom objects in fhir.
    /// </remarks>
    public Uri PrivateEndpoint { get; set; }

    /// <summary>
    /// Authentication settings for DICOMWeb.
    /// </summary>
    public AuthenticationConfiguration Authentication { get; set; }
}
