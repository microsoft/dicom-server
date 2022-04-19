// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="DicomElement"/>.
/// </summary>
public static class DicomElementExtensions
{
    /// <summary>
    /// Get first value of DicomElement if exists, otherwise return default(<typeparamref name="T"/>)
    /// </summary>
    /// <typeparam name="T">Value Type.</typeparam>
    /// <param name="dicomElement">The dicom element.</param>
    /// <returns>The value.</returns>
    public static T GetFirstValueOrDefault<T>(this DicomElement dicomElement)
    {
        EnsureArg.IsNotNull(dicomElement, nameof(dicomElement));
        if (dicomElement.Count == 0)
        {
            return default(T);
        }

        if (dicomElement.Count == 1)
        {
            return dicomElement.Get<T>();
        }
        return dicomElement.Get<T>(0);
    }

}
