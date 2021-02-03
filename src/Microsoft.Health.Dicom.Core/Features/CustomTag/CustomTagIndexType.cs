// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Index type of custom tag.
    /// </summary>
    public enum CustomTagIndexType
    {
        /// <summary>
        /// The custom tag is indexed as String.
        /// </summary>
        StringIndex = 1,

        /// <summary>
        /// The custom tag is indexed as Long.
        /// </summary>
        LongIndex = 2,

        /// <summary>
        /// The custom tag is indexed as Double.
        /// </summary>
        DoubleIndex = 3,

        /// <summary>
        /// The custom tag is indexed as DateTime.
        /// </summary>
        DateTimeIndex = 4,

        /// <summary>
        /// The custom tag is indexed as PersonName.
        /// </summary>
        PersonNameIndex = 5,
    }
}
