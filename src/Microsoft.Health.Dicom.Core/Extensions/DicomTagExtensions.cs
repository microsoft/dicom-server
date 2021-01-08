// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="DiomTag"/>.
    /// </summary>
    public static class DicomTagExtensions
    {
        /// <summary>
        /// Get path of given Dicom Tag.
        /// e.g:Path of Dicom tag (0008,0070) is 00080070
        /// </summary>
        /// <param name="dicomTag">The dicom tag</param>
        /// <returns>The path.</returns>
        public static string GetPath(this DicomTag dicomTag)
        {
            EnsureArg.IsNotNull(dicomTag, nameof(dicomTag));
            return dicomTag.Group.ToString("X4") + dicomTag.Element.ToString("X4");
        }
    }
}
