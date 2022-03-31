// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Core.Models.Export;

public class ExportDestination
{
    public ExportDestinationType Type { get; set; }

    public IReadOnlyDictionary<string, string> Configuration { get; set; }
}
