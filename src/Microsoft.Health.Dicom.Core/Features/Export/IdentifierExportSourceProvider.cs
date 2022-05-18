// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal sealed class IdentifierExportSourceProvider : ExportSourceProvider<IdentifierExportOptions>, IExportSourceProvider
{
    public override ExportSourceType Type => ExportSourceType.Identifiers;

    protected override Task<IExportSource> CreateAsync(IServiceProvider provider, IdentifierExportOptions options, PartitionEntry partition, CancellationToken cancellationToken = default)
        => Task.FromResult<IExportSource>(
            new IdentifierExportSource(
                provider.GetRequiredService<IInstanceStore>(),
                partition,
                Options.Create(options)));

    protected override Task ValidateAsync(IdentifierExportOptions options, CancellationToken cancellationToken = default)
    {
        List<ValidationResult> errors = options.Validate(new ValidationContext(this)).ToList();

        return errors.Count > 0
            ? Task.FromException(new ValidationException(errors.First().ErrorMessage))
            : Task.CompletedTask;
    }
}
