// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Client.Models.Export;

internal sealed class IdentifierExportOptions
{
    public IReadOnlyCollection<DicomIdentifier> Values { get; init; }
}
