// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Configs
{
    /// <summary>
    /// The type of transcoder
    /// </summary>
    public enum TranscoderType
    {
        /// <summary>
        /// The fodicom transcoder from https://github.com/fo-dicom/fo-dicom
        /// </summary>
        FoDicom,

        /// <summary>
        /// The efferent transcoder from https://github.com/Efferent-Health/fo-dicom.Codecs
        /// </summary>
        Efferent,
    }
}
