// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------


using System;

namespace Microsoft.Health.Dicom.Core.Configs;

public class ContentLengthBackFillConfiguration
{
    /// <summary>
    /// Gets or sets the operation id
    /// </summary>
    public Guid OperationId { get; set; } = Guid.Parse("1d7f8475-dea6-4ffb-be39-9ee7f7b89810");
}
