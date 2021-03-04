// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;

namespace Microsoft.Health.Dicom.Tests.Common.Extensions
{
    public static class IStoreOrchestratorExtensions
    {
        public static Task StoreDicomInstanceEntryAsync(
            this IStoreOrchestrator storeOrchestrator,
            IDicomInstanceEntry dicomInstanceEntry,
            CancellationToken cancellationToken = default)
        {
            return storeOrchestrator.StoreDicomInstanceEntryAsync(dicomInstanceEntry, new List<CustomTagEntry>(), cancellationToken);
        }
    }
}
