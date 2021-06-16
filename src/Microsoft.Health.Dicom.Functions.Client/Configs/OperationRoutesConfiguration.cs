// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Functions.Client.Configs
{
    public class OperationRoutesConfiguration
    {
        [Required]
        public string StatusTemplate { get; set; }

        [Required]
        public Uri StartReindex { get; set; }
    }
}
