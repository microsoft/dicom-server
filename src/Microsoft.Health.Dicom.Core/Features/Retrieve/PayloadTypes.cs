// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    [Flags]
    public enum PayloadTypes
    {
        None = 0,
        SinglePart = 1,
        MultipartRelated = 2,
    }
}
