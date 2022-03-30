// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal class IdentifierExportSourceProvider : IExportSourceProvider
{
    public ExportSourceType Type => ExportSourceType.Identifiers;

    public IExportSource Create(IServiceProvider provider, object input)
        => new IdentifierExportSource(input as IReadOnlyList<DicomIdentifier>, provider.GetRequiredService<IInstanceStore>());

    public void Validate(object input)
    {
        if (input is not IReadOnlyList<DicomIdentifier>)
            throw new InvalidOperationException();
    }
}
