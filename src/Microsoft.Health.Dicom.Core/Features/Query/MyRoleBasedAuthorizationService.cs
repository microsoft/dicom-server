// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core.Configs;
using Microsoft.Health.Core.Features.Context;

namespace Microsoft.Health.Core.Features.Security.Authorization;

/// <summary>
/// Service used for checking if given set of dataActions are present in a given request contexts principal.
/// </summary>
/// <typeparam name="TDataActions">Type representing the dataActions for the service</typeparam>
/// <typeparam name="TRequestContext">Type representing the IRequestContext implementation for the service</typeparam>
public class MyRoleBasedAuthorizationService<TDataActions, TRequestContext> : IAuthorizationService<TDataActions>
    where TDataActions : Enum
    where TRequestContext : IRequestContext
{
    private readonly RequestContextAccessor<TRequestContext> _requestContextAccessor;
    private readonly string _rolesClaimName;
    private readonly Dictionary<string, Role<TDataActions>> _roles;

    private static readonly Func<TDataActions, ulong> ConvertToULong = CreateConvertToULongFunc();
    private static readonly Func<ulong, TDataActions> ConvertToTDataAction = CreateConvertToTDataActionFunc();
    private readonly ILogger<MyRoleBasedAuthorizationService<TDataActions, TRequestContext>> _logger;

    public MyRoleBasedAuthorizationService(
        AuthorizationConfiguration<TDataActions> authorizationConfiguration,
        RequestContextAccessor<TRequestContext> requestContextAccessor,
        ILogger<MyRoleBasedAuthorizationService<TDataActions, TRequestContext>> logger)
    {
        EnsureArg.IsNotNull(authorizationConfiguration, nameof(authorizationConfiguration));
        _requestContextAccessor = EnsureArg.IsNotNull(requestContextAccessor, nameof(requestContextAccessor));

        _rolesClaimName = authorizationConfiguration.RolesClaim;
        _roles = authorizationConfiguration.Roles.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    private static Func<TDataActions, ulong> CreateConvertToULongFunc()
    {
        ParameterExpression parameterExpression = Expression.Parameter(typeof(TDataActions));
        return Expression.Lambda<Func<TDataActions, ulong>>(Expression.Convert(parameterExpression, typeof(ulong)), parameterExpression).Compile();
    }

    private static Func<ulong, TDataActions> CreateConvertToTDataActionFunc()
    {
        ParameterExpression parameterExpression = Expression.Parameter(typeof(ulong));
        return Expression.Lambda<Func<ulong, TDataActions>>(Expression.Convert(parameterExpression, typeof(TDataActions)), parameterExpression).Compile();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "<Pending>")]
    public ValueTask<TDataActions> CheckAccess(TDataActions dataActions, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PENCHE Show all claims");
        ClaimsPrincipal principal = _requestContextAccessor.RequestContext.Principal;
        foreach (var item in principal.Claims)
        {
            _logger.LogInformation($"PENCHE claim: {item}");
        }

        ulong permittedDataActions = 0;
        ulong dataActionsUlong = ConvertToULong(dataActions);
        _logger.LogInformation("PENCHE Show qualified claims");
        foreach (Claim claim in principal.FindAll(_rolesClaimName))
        {
            _logger.LogInformation($"PENCHE {_rolesClaimName} claim: {claim}");
            if (_roles.TryGetValue(claim.Value, out Role<TDataActions> role))
            {
                permittedDataActions |= role.AllowedDataActionsUlong;
                if (permittedDataActions == dataActionsUlong)
                {
                    break;
                }
            }
        }

        return new ValueTask<TDataActions>(ConvertToTDataAction(dataActionsUlong & permittedDataActions));
    }
}
