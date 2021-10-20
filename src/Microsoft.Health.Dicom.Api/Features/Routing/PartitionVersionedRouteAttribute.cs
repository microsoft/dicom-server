// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
namespace Microsoft.Health.Dicom.Api.Features.Routing
{
    public sealed class PartitionVersionedRouteAttribute : RouteAttribute
    {
        public PartitionVersionedRouteAttribute(string template)
            : base("/v{version:apiVersion}/" + KnownRoutes.PartitionRoute + "/" + template)
        {
        }
    }
}
