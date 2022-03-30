// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal class AzureBlobExportSink : IExportSink
{
    public Uri ErrorHref => throw new NotImplementedException();

    public Task AppendErrorAsync(VersionedInstanceIdentifier source, Exception exception)
    {
        throw new NotImplementedException();
    }

    public Task CopyAsync(VersionedInstanceIdentifier source)
    {
        throw new NotImplementedException();
    }
}
