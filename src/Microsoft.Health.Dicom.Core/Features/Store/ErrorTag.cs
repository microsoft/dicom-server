// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Store;

public sealed class ErrorTag
{
    public ErrorTag(DicomTag dicomTag, bool isCoreTag)
    {
        DicomTag = dicomTag;
        IsCoreTag = isCoreTag;
    }

    public DicomTag DicomTag { get; }

    public bool IsCoreTag { get; }
}
