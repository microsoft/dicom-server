// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage
{
    public static class IDicomMetadataStoreExtensions
    {
        public static async Task AddStudySeriesDicomMetadataAsync(this IDicomMetadataStore metadataStore, DicomDataset instance, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
            EnsureArg.IsNotNull(instance, nameof(instance));
            await metadataStore.AddStudySeriesDicomMetadataAsync(new[] { instance }, cancellationToken);
        }
    }
}
