// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    /// <summary>
    /// Provides functionality to parse dicom tag path
    /// </summary>
    public interface IDicomTagParser
    {
        /// <summary>
        /// Parse dicom tag path.
        /// </summary>
        /// <param name="dicomTagPath">The dicom tag path.</param>
        /// <param name="dicomTags">The parsed dicom tags.</param>
        /// <param name="supportMultiple">True to support multiple dicom tags like '00101002.00100024.00400032'.</param>
        /// <returns>True if succeed.</returns>
        bool TryParse(string dicomTagPath, out DicomTag[] dicomTags, bool supportMultiple = false);
    }
}
