// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Models
{
    public class CohortResource
    {
        public string ResourceId { get; set; }
        public CohortResourceType ResourceType { get; set; }
#pragma warning disable CA1056 // URI-like properties should not be strings
        public string ReferenceUrl { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings
    }
}
