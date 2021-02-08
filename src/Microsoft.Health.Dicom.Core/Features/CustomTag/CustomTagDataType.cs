// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Date type of custom tag.
    /// </summary>
    public enum CustomTagDataType
    {
        /// <summary>
        /// The custom tag is treated as String.
        /// </summary>
        StringData = 1,

        /// <summary>
        /// The custom tag is treated as Long.
        /// </summary>
        LongData = 2,

        /// <summary>
        /// The custom tag is treated as Double.
        /// </summary>
        DoubleData = 3,

        /// <summary>
        /// The custom tag is treated as DateTime.
        /// </summary>
        DateTimeData = 4,

        /// <summary>
        /// The custom tag is treated as PersonName.
        /// </summary>
        PersonNameData = 5,
    }
}
