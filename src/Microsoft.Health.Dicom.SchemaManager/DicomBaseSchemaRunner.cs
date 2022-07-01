// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;

namespace Microsoft.Health.Dicom.SchemaManager;

public class DicomBaseSchemaRunner : IBaseSchemaRunner
{
    private readonly BaseSchemaRunner _baseSchemaRunner;
    private readonly ILogger<BaseSchemaRunner> _logger;

    public DicomBaseSchemaRunner(BaseSchemaRunner baseSchemaRunner,
        ILogger<BaseSchemaRunner> logger)
    {
        _baseSchemaRunner = EnsureArg.IsNotNull(baseSchemaRunner, nameof(baseSchemaRunner));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task EnsureBaseSchemaExistsAsync(CancellationToken cancellationToken)
    {
        await _baseSchemaRunner.EnsureBaseSchemaExistsAsync(cancellationToken);
    }

    public async Task EnsureInstanceSchemaRecordExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _baseSchemaRunner.EnsureInstanceSchemaRecordExistsAsync(cancellationToken);
        }
        catch (SchemaManagerException ex) when (ex.Message.Equals("The current version information could not be fetched from the service. Please try again.", StringComparison.Ordinal))
        {
            // TODO: Throw a more exact error message type in the baseSchemaRunner
            _logger.LogInformation("There was no current information found, this is a new DB.");
        }
    }
}
