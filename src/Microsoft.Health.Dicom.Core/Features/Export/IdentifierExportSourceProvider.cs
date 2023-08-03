// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal sealed class IdentifierExportSourceProvider : ExportSourceProvider<IdentifierExportOptions>, IExportSourceProvider
{
    public override ExportSourceType Type => ExportSourceType.Identifiers;

    private readonly IInstanceStore _instanceStore;

    public IdentifierExportSourceProvider(IInstanceStore instanceStore)
        => _instanceStore = EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Callers will dipose of source.")]
    protected override Task<IExportSource> CreateAsync(IdentifierExportOptions options, Partition partition, CancellationToken cancellationToken = default)
        => Task.FromResult<IExportSource>(new IdentifierExportSource(_instanceStore, partition, options));

    protected override Task ValidateAsync(IdentifierExportOptions options, CancellationToken cancellationToken = default)
    {
        List<ValidationResult> errors = options.Validate(new ValidationContext(this)).ToList();

        return errors.Count > 0
            ? Task.FromException(new ValidationException(errors.First().ErrorMessage))
            : Task.CompletedTask;
    }
}
