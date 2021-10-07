// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Client.Models
{
    /// <summary>
    /// Encapsulate parameters for updating extended query tag.
    /// </summary>
    public class UpdateExtendedQueryTagEntry
    {
        /// <summary>
        /// Gets or sets query status.
        /// </summary>
        public QueryStatus QueryStatus { get; set; }
    }
}
