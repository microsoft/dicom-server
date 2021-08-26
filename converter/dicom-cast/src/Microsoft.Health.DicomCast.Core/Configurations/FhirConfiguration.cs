// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.DicomCast.Core.Configurations
{
    public class FhirConfiguration
    {
        public Uri Endpoint { get; set; }

        /// <summary>
        /// Authentication settings for the FHIR server.
        /// </summary>
        public AuthenticationConfiguration Authentication { get; set; }

        public Uri BlobEndpoint { get; set; }
    }
}
