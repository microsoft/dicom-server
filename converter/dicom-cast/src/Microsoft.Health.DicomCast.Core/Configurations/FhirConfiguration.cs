// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Client.Configuration;

namespace Microsoft.Health.DicomCast.Core.Configurations;

public class FhirConfiguration
{
    public Uri Endpoint { get; set; }

    /// <summary>
    /// Authentication settings for the FHIR server.
    /// </summary>
    public AuthenticationConfiguration Authentication { get; set; }
}
