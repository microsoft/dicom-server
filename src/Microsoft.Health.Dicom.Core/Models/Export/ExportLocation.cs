// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Microsoft.Health.Dicom.Core.Models.Export;

public class ExportLocation
{
    public ExportDestinationType Type { get; set; }

    public IConfiguration Configuration { get; set; }
}
