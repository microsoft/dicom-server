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
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal sealed class IdentifierExportSourceProvider : IExportSourceProvider
{
    public ExportSourceType Type => ExportSourceType.Identifiers;

    public Task<IExportSource> CreateAsync(IServiceProvider provider, IConfiguration config, PartitionEntry partition, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(provider, nameof(provider));
        EnsureArg.IsNotNull(config, nameof(config));
        EnsureArg.IsNotNull(partition, nameof(partition));

        var options = new IdentifierExportOptions();
        config.Bind(options);

        return Task.FromResult<IExportSource>(
            new IdentifierExportSource(
                provider.GetRequiredService<IInstanceStore>(),
                partition,
                Options.Create(options)));
    }

    public Task<IConfiguration> ValidateAsync(IConfiguration config, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(config, nameof(config));

        var options = new IdentifierExportOptions();
        config.Bind(options);

        List<ValidationResult> errors = options.Validate(new ValidationContext(this)).ToList();

        return errors.Count > 0
            ? Task.FromException<IConfiguration>(new ValidationException(errors.First().ErrorMessage))
            : Task.FromResult(config);
    }
}
