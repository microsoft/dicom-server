// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal class AzureBlobExportSink : IExportSink
{
    private readonly IBlobCopyStore _copyStore;

    public AzureBlobExportSink(IBlobCopyStore copyStore)
    {
        EnsureArg.IsNotNull(copyStore, nameof(copyStore));

        _copyStore = copyStore;
    }

    public Task<Uri> GetErrorHrefAsync(CancellationToken cancellationToken)
    {
        return _copyStore.GetErrorHrefAsync(cancellationToken);
    }

    public Task CopyAsync(VersionedInstanceIdentifier source, CancellationToken cancellationToken)
    {
        return _copyStore.CopyFileAsync(source, cancellationToken);
    }

    public Task AppendErrorAsync(Stream errorContent, CancellationToken cancellationToken)
    {
        return _copyStore.AppendErrorLogAsync(errorContent, cancellationToken);
    }
}
