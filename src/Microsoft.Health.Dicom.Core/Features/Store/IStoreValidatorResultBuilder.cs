// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Store;

public interface IStoreValidatorResultBuilder
{
    StoreValidatorResult Build();

    void AddError(Exception ex, QueryTag queryTag = null);

    void AddError(string message, QueryTag queryTag = null);

    void AddWarning(ValidationWarnings warningCode, QueryTag queryTag = null);
}
