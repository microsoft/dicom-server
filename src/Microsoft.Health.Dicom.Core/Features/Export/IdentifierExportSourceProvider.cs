// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal sealed class IdentifierExportSourceProvider : IExportSourceProvider
{
    public ExportSourceType Type => ExportSourceType.Identifiers;

    public IExportSource Create(IServiceProvider provider, IConfiguration config, PartitionEntry partition)
    {
        EnsureArg.IsNotNull(provider, nameof(provider));
        EnsureArg.IsNotNull(config, nameof(config));
        EnsureArg.IsNotNull(partition, nameof(partition));

        return new IdentifierExportSource(
            provider.GetRequiredService<IInstanceStore>(),
            partition,
            config.Get<IdentifierExportOptions>());
    }

    public void Validate(IConfiguration config)
    {
        EnsureArg.IsNotNull(config, nameof(config));

        IdentifierExportOptions options = config.Get<IdentifierExportOptions>();
        List<ValidationResult> errors = options.Validate(new ValidationContext(this)).ToList();

        if (errors.Count > 0)
            throw new ValidationException(errors.First().ErrorMessage);
    }
}
