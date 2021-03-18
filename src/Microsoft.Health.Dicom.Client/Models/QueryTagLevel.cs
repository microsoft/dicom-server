// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Client.Models
{
    /// <summary>
    /// Level of a queryable tag.
    /// </summary>
    public enum QueryTagLevel
    {
        /// <summary>
        /// The tag is queriable on instance level.
        /// </summary>
        Instance = 0,

        /// <summary>
        /// The tag is queriable on series level.
        /// </summary>
        Series = 1,

        /// <summary>
        /// The tag is queriable on study level.
        /// </summary>
        Study = 2,
    }
}
