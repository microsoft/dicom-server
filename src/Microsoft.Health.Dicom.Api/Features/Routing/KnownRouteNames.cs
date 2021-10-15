// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Api.Features.Routing
{
    internal class KnownRouteNames
    {
        internal const string VersionedRetrieveStudy = nameof(VersionedRetrieveStudy);
        internal const string PartitionVersionedRetrieveStudy = nameof(PartitionVersionedRetrieveStudy);
        internal const string RetrieveStudy = nameof(RetrieveStudy);
        internal const string PartitionRetrieveStudy = nameof(PartitionRetrieveStudy);

        internal const string VersionedRetrieveSeries = nameof(VersionedRetrieveSeries);
        internal const string PartitionVersionedRetrieveSeries = nameof(PartitionVersionedRetrieveSeries);
        internal const string RetrieveSeries = nameof(RetrieveSeries);
        internal const string PartitionRetrieveSeries = nameof(PartitionRetrieveSeries);

        internal const string PartitionVersionedRetrieveInstance = nameof(PartitionVersionedRetrieveInstance);
        internal const string PartitionRetrieveInstance = nameof(PartitionRetrieveInstance);
        internal const string VersionedRetrieveInstance = nameof(VersionedRetrieveInstance);
        internal const string RetrieveInstance = nameof(RetrieveInstance);

        internal const string PartitionVersionedRetrieveFrame = nameof(PartitionVersionedRetrieveFrame);
        internal const string PartitionRetrieveFrame = nameof(PartitionRetrieveFrame);
        internal const string VersionedRetrieveFrame = nameof(VersionedRetrieveFrame);
        internal const string RetrieveFrame = nameof(RetrieveFrame);

        internal const string VersionedOperationStatus = nameof(VersionedOperationStatus);
        internal const string OperationStatus = nameof(OperationStatus);

        internal const string VersionedGetExtendedQueryTag = nameof(VersionedGetExtendedQueryTag);
        internal const string GetExtendedQueryTag = nameof(GetExtendedQueryTag);

        internal const string VersionedGetExtendedQueryTagErrors = nameof(VersionedGetExtendedQueryTagErrors);
        internal const string GetExtendedQueryTagErrors = nameof(GetExtendedQueryTagErrors);
    }
}
