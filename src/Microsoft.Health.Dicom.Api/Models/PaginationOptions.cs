// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Api.Models
{
    public class PaginationOptions
    {
        [Range(0, int.MaxValue)]
        public int Offset { get; set; }

        [Range(1, 200)]
        public int Limit { get; set; } = 100;
    }
}
