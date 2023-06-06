// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Core.Features.Query;

public class BaseQueryParameters
{
    public IReadOnlyDictionary<string, string> Filters { get; set; }

    public long Offset { get; set; }

    public int Limit { get; set; } = 100;

    public bool FuzzyMatching { get; set; }

    public IReadOnlyList<string> IncludeField { get; set; }
}
