// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="DicomDataset"/>.
/// </summary>
public static class DicomElementExtensions
{
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
