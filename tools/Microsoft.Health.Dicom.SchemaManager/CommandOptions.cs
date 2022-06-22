// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.CommandLine;
using Microsoft.Health.Dicom.SchemaManager.Properties;

namespace Microsoft.Health.Dicom.SchemaManager;

public static class CommandOptions
{
    public static Option ConnectionStringOption()
    {
        var connectionStringOption = new Option<string>(
            name: OptionAliases.ConnectionString,
            description: Resources.ConnectionStringOptionDescription)
        {
            Arity = ArgumentArity.ExactlyOne,
            IsRequired = true
        };

        connectionStringOption.AddAlias(OptionAliases.ShortConnectionString);

        return connectionStringOption;
    }

    public static Option ManagedIdentityClientIdOption()
    {
        var managedIdentityClientIdOption = new Option<string>(
            name: OptionAliases.ManagedIdentityClientId,
            description: Resources.ManagedIdentityClientIdDescription)
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        managedIdentityClientIdOption.AddAlias(OptionAliases.ShortManagedIdentityClientId);

        return managedIdentityClientIdOption;
    }

    public static Option AuthenticationTypeOption()
    {
        var connectionStringOption = new Option<string>(
            name: OptionAliases.AuthenticationType,
            description: Resources.AuthenticationTypeDescription)
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        connectionStringOption.AddAlias(OptionAliases.ShortAuthenticationType);

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
