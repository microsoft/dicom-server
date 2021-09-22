// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Encapsulate parameters for updating extended query tag.
    /// </summary>
    public class UpdateExtendedQueryTagEntry
    {
        /// <summary>
        /// Gets or sets query status.
        /// </summary>
        [Required]
        public QueryStatus? QueryStatus { get; set; }

        public override string ToString()
        {
            return $"QueryStatus: {QueryStatus}";
        }
    }
}
