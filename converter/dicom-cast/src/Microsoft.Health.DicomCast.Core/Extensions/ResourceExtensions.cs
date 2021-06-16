// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;

namespace Microsoft.Health.DicomCast.Core.Extensions
{
    public static class ResourceExtensions
    {
        public static ServerResourceId ToServerResourceId(this Resource resource)
        {
            EnsureArg.IsNotNull(resource, nameof(resource));

            return new ServerResourceId(resource.TypeName, resource.Id);
        }
    }
}
