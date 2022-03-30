// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

public interface IExportSinkProvider
{
    ExportDestinationType Type { get; }

    IExportSink Create(IServiceProvider provider, IConfiguration config);

    void Validate(IConfiguration config);
}
