// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Core.Features.Export;


// TODO: better design
public class ExportOperationSource
{
    public ExportOperationSource(ISet<string> studies, IDictionary<string, ISet<string>> series, IDictionary<string, IDictionary<string, ISet<string>>> instances)
    {
        Studies = studies;
        Series = series;
        Instances = instances;
    }
    public ISet<string> Studies { get; }

    public IDictionary<string, ISet<string>> Series { get; }

    public IDictionary<string, IDictionary<string, ISet<string>>> Instances { get; }
}
