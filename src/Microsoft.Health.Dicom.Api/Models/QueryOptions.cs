// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Dicom.Api.Features.ModelBinders;

namespace Microsoft.Health.Dicom.Api.Models
{
    public class QueryOptions
    {
        [Range(0, int.MaxValue)]
        public int Offset { get; set; }

        [Range(1, 200)]
        public int Limit { get; set; } = 100;

        public bool FuzzyMatching { get; set; }

        [ModelBinder(typeof(CsvModelBinder))]
        public IReadOnlyList<string> IncludeField { get; set; } = Array.Empty<string>();
    }
}
