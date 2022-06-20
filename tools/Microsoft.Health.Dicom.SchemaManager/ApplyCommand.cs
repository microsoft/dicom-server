// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.SchemaManager.Properties;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;

namespace Microsoft.Health.Dicom.SchemaManager;

public class ApplyCommand : Command
{
    private readonly ISchemaManager _schemaManager;
    private readonly ILogger<ApplyCommand> _logger;

    public ApplyCommand(
        ISchemaManager schemaManager,
        ILogger<ApplyCommand> logger)
        : base("apply", Resources.ApplyCommandDescription)
    {
        AddOption(CommandOptions.ConnectionStringOption());
        AddOption(CommandOptions.ServerOption());
        AddOption(CommandOptions.VersionOption());
        AddOption(CommandOptions.NextOption());
        AddOption(CommandOptions.LatestOption());
        AddOption(CommandOptions.ForceOption());

        Handler = CommandHandler.Create(
            (MutuallyExclusiveType type, bool force, CancellationToken token)
            => ApplyHandler(type, force, token));

        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(schemaManager, nameof(schemaManager));

        _schemaManager = schemaManager;
        _logger = logger;
    }

    private Task ApplyHandler(MutuallyExclusiveType type, bool force, CancellationToken token = default)
    {
        if (force && !EnsureForce())
        {
            return Task.CompletedTask;
        }

        return _schemaManager.ApplySchema(type, token);
    }
    private bool EnsureForce()
    {
        _logger.LogWarning("Are you sure to apply command with force option? Type 'yes' to confirm.");
        return string.Equals(Console.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase);
    }

}
