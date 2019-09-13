// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public interface IDicomBlobDataStore
    {
        Task<bool> InstanceExistsAsync(DicomInstance dicomInstance, CancellationToken cancellationToken = default);

        Task<Uri> AddInstanceAsStreamAsync(DicomInstance dicomInstance, Stream buffer, bool overwriteIfExists = false, CancellationToken cancellationToken = default);

        Task<Stream> GetInstanceAsStreamAsync(DicomInstance dicomInstance, CancellationToken cancellationToken = default);

        Task DeleteInstanceIfExistsAsync(DicomInstance dicomInstance, CancellationToken cancellationToken = default);
    }
}
