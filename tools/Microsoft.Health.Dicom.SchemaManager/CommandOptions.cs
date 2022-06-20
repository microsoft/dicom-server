// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.CommandLine;
using Microsoft.Health.Dicom.SchemaManager.Properties;

namespace Microsoft.Health.Dicom.SchemaManager;

public static class CommandOptions
{
    public static Option ServerOption()
    {
        var serverOption = new Option<Uri>(
            name: OptionAliases.Server,
            description: Resources.ServerOptionDescription)
        {
            Arity = ArgumentArity.ExactlyOne
        };

        serverOption.AddAlias(OptionAliases.ShortServer);

        return serverOption;
    }

    public static Option ConnectionStringOption()
    {
        var connectionStringOption = new Option<string>(
            name: OptionAliases.ConnectionString,
            description: Resources.ConnectionStringOptionDescription)
        {
            Arity = ArgumentArity.ExactlyOne
        };

        connectionStringOption.AddAlias(OptionAliases.ShortConnectionString);

        return connectionStringOption;
    }

    public static Option VersionOption()
    {
        var versionOption = new Option<int>(
            name: OptionAliases.Version,
            description: Resources.VersionOptionDescription)
        {
            Arity = ArgumentArity.ExactlyOne
        };

        versionOption.AddAlias(OptionAliases.ShortVersion);

        return versionOption;
    }

    public static Option NextOption()
    {
        var nextOption = new Option<bool>(
            name: OptionAliases.Next,
            description: Resources.NextOptionDescritpion)
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        nextOption.AddAlias(OptionAliases.ShortNext);

        return nextOption;
    }

    public static Option LatestOption()
    {
        var latestOption = new Option<bool>(
            name: OptionAliases.Latest,
            description: Resources.LatestOptionDescription)
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        latestOption.AddAlias(OptionAliases.ShortLatest);

        return latestOption;
    }

    public static Option ForceOption()
    {
        var forceOption = new Option<bool>(
            name: OptionAliases.Force,
            description: Resources.ForceOptionDescription)
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        forceOption.AddAlias(OptionAliases.ShortForce);

        return forceOption;
    }
}
