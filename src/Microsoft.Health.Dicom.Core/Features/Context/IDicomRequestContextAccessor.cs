// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Core.Features.Context;

namespace Microsoft.Health.Dicom.Core.Features.Context
{
    public interface IDicomRequestContextAccessor
    {
        IRequestContext DicomRequestContext { get; set; }
    }
}
