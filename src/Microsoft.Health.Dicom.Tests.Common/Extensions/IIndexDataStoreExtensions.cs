// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Tests.Common.Extensions
{
    public static class IIndexDataStoreExtensions
    {
        public static Task<long> BeginCreateInstanceIndexAsync(this IIndexDataStore indexDataStore, DicomDataset dicomDataset, string partitionName = DefaultPartition.Name, CancellationToken cancellationToken = default)
            => indexDataStore.BeginCreateInstanceIndexAsync(dicomDataset, Array.Empty<QueryTag>(), partitionName, cancellationToken);

        public static Task EndCreateInstanceIndexAsync(this IIndexDataStore indexDataStore, DicomDataset dicomDataset, long watermark, string partitionName = DefaultPartition.Name, CancellationToken cancellationToken = default)
            => indexDataStore.EndCreateInstanceIndexAsync(dicomDataset, watermark, Array.Empty<QueryTag>(), partitionName, true, cancellationToken);
    }
}
