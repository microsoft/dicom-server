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
        public static string GetPath(this DicomTag dicomTag)
        {
            EnsureArg.IsNotNull(dicomTag, nameof(dicomTag));
            return dicomTag.Group.ToString("X2") + dicomTag.Element.ToString("X2");
        }
    }
}
