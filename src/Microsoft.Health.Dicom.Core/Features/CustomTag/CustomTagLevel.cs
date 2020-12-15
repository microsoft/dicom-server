// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Level of a custom tag.
    /// </summary>
    public enum CustomTagLevel
    {
        /// <summary>
        /// The custom tag is on instance level.
        /// </summary>
        Instance = 1,

        /// <summary>
        /// The custom tag is on series level.
        /// </summary>
        Series = 2,

        /// <summary>
        /// The custom tag is on study level.
        /// </summary>
        Study = 3,
    }
}
