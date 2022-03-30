// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Core.Features.Export;
public class ExportSeries
{
    public string StudyUid { get; set; }

    public IReadOnlyList<string> SeriesUids { get; set; }
}
