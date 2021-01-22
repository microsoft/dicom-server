// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Level of a custom tag.
    /// </summary>
    public enum CustomTagJobStatus
    {
        Queued = 0,
        Executing = 1,
        Error = 2,
    }
}
