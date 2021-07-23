// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Api.Features.Routing
{
    internal class KnownRouteNames
    {
        internal const string VersionedRetrieveStudy = nameof(VersionedRetrieveStudy);
        internal const string RetrieveStudy = nameof(RetrieveStudy);

        internal const string VersionedRetrieveSeries = nameof(VersionedRetrieveSeries);
        internal const string RetrieveSeries = nameof(RetrieveSeries);

        internal const string VersionedRetrieveInstance = nameof(VersionedRetrieveInstance);
        internal const string RetrieveInstance = nameof(RetrieveInstance);

        internal const string VersionedRetrieveFrame = nameof(VersionedRetrieveFrame);
        internal const string RetrieveFrame = nameof(RetrieveFrame);

        internal const string OperationStatus = nameof(OperationStatus);
    }
}
