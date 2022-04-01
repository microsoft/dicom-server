// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;

public interface IBlobCopyStore : IDisposable
{
    Task AppendErrorLogAsync(Stream content, CancellationToken cancellationToken);

    Task CopyFileAsync(VersionedInstanceIdentifier instanceIdentifier, CancellationToken cancellationToken);

    Task<Uri> GetErrorHrefAsync(CancellationToken cancellationToken);
}
