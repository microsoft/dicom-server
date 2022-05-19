// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Client.Models;

internal class ExportSpecification
{
    public ExportDataOptions<ExportSourceType> Source { get; init; }

    public ExportDataOptions<ExportDestinationType> Destination { get; init; }
}
