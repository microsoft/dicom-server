// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Export;

public interface IExportSink
{
    Uri ErrorHref { get; }

    Task CopyAsync(VersionedInstanceIdentifier source);

    Task AppendErrorAsync(VersionedInstanceIdentifier source, Exception exception);
}
