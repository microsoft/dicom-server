// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Dicom.Api.Features.ModelBinders;

namespace Microsoft.Health.Dicom.Api.Models
{
    public class QueryOptions : PaginationOptions
    {
        public bool FuzzyMatching { get; set; }

        [ModelBinder(typeof(AggregateCsvModelBinder))]
        public IReadOnlyList<string> IncludeField { get; set; } = Array.Empty<string>();
    }
}
