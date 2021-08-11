// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Tests.Common.Extensions
{
    public static class IIndexDataStoreExtensions
    {
        public static Task<long> CreateInstanceIndexAsync(this IIndexDataStore indexDataStore, DicomDataset dicomDataset, CancellationToken cancellationToken = default)
        {
            return indexDataStore.CreateInstanceIndexAsync(dicomDataset, Array.Empty<QueryTag>(), cancellationToken);
        }
    }
}
