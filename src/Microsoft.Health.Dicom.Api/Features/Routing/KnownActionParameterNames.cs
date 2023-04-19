// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Api.Features.Routing;

internal static class KnownActionParameterNames
{
    internal const string Version = "version";
    internal const string PartitionName = "partitionName";
    internal const string StudyInstanceUid = "studyInstanceUid";
    internal const string SeriesInstanceUid = "seriesInstanceUid";
    internal const string SopInstanceUid = "sopInstanceUid";
    internal const string WorkItemInstanceUid = "workitemInstanceUid";
    internal const string TransactionUid = "transactionUid";
    internal const string Frames = "frames";
    internal const string Frame = "frame";
    internal const string TagPath = "tagPath";
    internal const string OperationId = "operationId";
}
