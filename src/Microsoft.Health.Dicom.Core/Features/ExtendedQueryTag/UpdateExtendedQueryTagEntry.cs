// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class UpdateExtendedQueryTagEntry
    {
        [Required(AllowEmptyStrings = false)]
        public QueryStatus? QueryStatus { get; set; }

        public override string ToString()
        {
            return $"QueryStatus: {QueryStatus}";
        }
    }
}
