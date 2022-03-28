// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Core.Features.Export;
public class ExportOperationSource
{
    public IReadOnlySet<string> ExportStudies { get; set; }

    public IReadOnlyDictionary<string, IReadOnlySet<string>> ExportSeries { get; set; }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlySet<string>>> ExportInstances { get; set; }
}
