// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Api.Features.Routing;

internal class KnownRouteNames
{
    internal const string RetrieveStudy = nameof(RetrieveStudy);
    internal const string PartitionRetrieveStudy = nameof(PartitionRetrieveStudy);

    internal const string PartitionRetrieveSeries = nameof(PartitionRetrieveSeries);
    internal const string RetrieveSeries = nameof(RetrieveSeries);

    internal const string PartitionRetrieveInstance = nameof(PartitionRetrieveInstance);
    internal const string RetrieveInstance = nameof(RetrieveInstance);

    internal const string PartitionRetrieveFrame = nameof(PartitionRetrieveFrame);
    internal const string RetrieveFrame = nameof(RetrieveFrame);

    internal const string OperationStatus = nameof(OperationStatus);

    internal const string GetExtendedQueryTag = nameof(GetExtendedQueryTag);

    internal const string GetExtendedQueryTagErrors = nameof(GetExtendedQueryTagErrors);

    internal const string PartitionStoreInstance = nameof(PartitionStoreInstance);
    internal const string StoreInstance = nameof(StoreInstance);

    internal const string PartitionStoreInstancesInStudy = nameof(PartitionStoreInstancesInStudy);
    internal const string StoreInstancesInStudy = nameof(StoreInstancesInStudy);

    internal const string PartitionedAddWorkitemInstance = nameof(PartitionedAddWorkitemInstance);
    internal const string AddWorkitemInstance = nameof(AddWorkitemInstance);

    internal const string PartitionedRetrieveWorkitemInstance = nameof(PartitionedRetrieveWorkitemInstance);
    internal const string RetrieveWorkitemInstance = nameof(RetrieveWorkitemInstance);

    internal const string VersionedPartitionUpdateWorkitemInstance = nameof(VersionedPartitionUpdateWorkitemInstance);
    internal const string PartitionedUpdateWorkitemInstance = nameof(PartitionedUpdateWorkitemInstance);
    internal const string VersionedUpdateWorkitemInstance = nameof(VersionedUpdateWorkitemInstance);
    internal const string UpdateWorkitemInstance = nameof(UpdateWorkitemInstance);

    internal const string VersionedPartitionCancelWorkitemInstance = nameof(VersionedPartitionCancelWorkitemInstance);
    internal const string PartitionedCancelWorkitemInstance = nameof(PartitionedCancelWorkitemInstance);
    internal const string CancelWorkitemInstance = nameof(CancelWorkitemInstance);
}
