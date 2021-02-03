// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Level of a custom tag.
    /// </summary>
    public enum CustomTagIndexType
    {
        Unknown = 0,

        /// <summary>
        /// The custom tag is on instance level.
        /// </summary>
        StringIndex = 1,

        /// <summary>
        /// The custom tag is on series level.
        /// </summary>
        LongIndex = 2,

        /// <summary>
        /// The custom tag is on study level.
        /// </summary>
        DoubleIndex = 3,

        DateTimeIndex = 4,

        PersonNameIndex = 5,
    }
}
