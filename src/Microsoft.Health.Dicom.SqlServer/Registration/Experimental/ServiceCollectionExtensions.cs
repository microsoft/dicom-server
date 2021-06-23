// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Extensions;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        private const string SqlServerSectionName = "SqlServer";

        public static IServiceCollection AddSqlServer(
            this IServiceCollection services,
            IConfiguration configurationRoot,
            Action<ISqlServiceBuilder> configureSqlServer = null)
        {
            EnsureArg.IsNotNull(services, nameof(services));
            EnsureArg.IsNotNull(configurationRoot, nameof(configurationRoot));

            var config = new SqlServerDataStoreConfiguration();
            configurationRoot.GetSection(SqlServerSectionName).Bind(config);

            services
                .AddSingleton(config)
                .AddConnectionServices(config);

            configureSqlServer?.Invoke(new SqlServiceBuilder(services));

            return services;
        }

        private static IServiceCollection AddConnectionServices(
            this IServiceCollection services,
            SqlServerDataStoreConfiguration config)
        {
            services.Add<SqlTransactionHandler>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlServerTransientFaultRetryPolicyFactory>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<RetrySqlCommandWrapperFactory>()
                .Singleton()
                .AsSelf()
                .AsService<SqlCommandWrapperFactory>();

            services.Add<SqlConnectionWrapperFactory>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.AddSingleton<ISqlConnectionStringProvider, DefaultSqlConnectionStringProvider>();

            switch (config.AuthenticationType)
            {
                case SqlServerAuthenticationType.ManagedIdentity:
                    services.AddSingleton<ISqlConnectionFactory, ManagedIdentitySqlConnectionFactory>();
                    services.AddSingleton<IAccessTokenHandler, ManagedIdentityAccessTokenHandler>();
                    services.AddSingleton<AzureServiceTokenProvider>();
                    break;
                case SqlServerAuthenticationType.ConnectionString:
                default:
                    services.AddSingleton<ISqlConnectionFactory, DefaultSqlConnectionFactory>();
                    break;
            }

            return services;
        }

        #region Shared Component Copies (TODO: Remove)

        private sealed class RetrySqlCommandWrapper : SqlCommandWrapper
        {
            private readonly SqlCommandWrapper _sqlCommandWrapper;
            private readonly IAsyncPolicy _retryPolicy;

            public RetrySqlCommandWrapper(SqlCommandWrapper sqlCommandWrapper, IAsyncPolicy retryPolicy)
                : base(sqlCommandWrapper)
            {
                EnsureArg.IsNotNull(sqlCommandWrapper, nameof(sqlCommandWrapper));
                EnsureArg.IsNotNull(retryPolicy, nameof(retryPolicy));

                _sqlCommandWrapper = sqlCommandWrapper;
                _retryPolicy = retryPolicy;
            }

            /// <inheritdoc/>
            public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
                => _retryPolicy.ExecuteAsync(() => _sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken));

            /// <inheritdoc/>
            public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
                => _retryPolicy.ExecuteAsync(() => _sqlCommandWrapper.ExecuteScalarAsync(cancellationToken));

            /// <inheritdoc/>
            public override Task<SqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
                => _retryPolicy.ExecuteAsync(() => _sqlCommandWrapper.ExecuteReaderAsync(cancellationToken));

            /// <inheritdoc/>
            public override Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
                => _retryPolicy.ExecuteAsync(() => _sqlCommandWrapper.ExecuteReaderAsync(behavior, cancellationToken));
        }

        private sealed class RetrySqlCommandWrapperFactory : SqlCommandWrapperFactory
        {
            private readonly IAsyncPolicy _retryPolicy;

            public RetrySqlCommandWrapperFactory(ISqlServerTransientFaultRetryPolicyFactory sqlTransientFaultRetryPolicyFactory)
            {
                EnsureArg.IsNotNull(sqlTransientFaultRetryPolicyFactory, nameof(sqlTransientFaultRetryPolicyFactory));

                _retryPolicy = sqlTransientFaultRetryPolicyFactory.Create();
            }

            /// <inheritdoc/>
            public override SqlCommandWrapper Create(SqlCommand sqlCommand)
            {
                return new RetrySqlCommandWrapper(
                    base.Create(sqlCommand),
                    _retryPolicy);
            }
        }

        private sealed class SqlServerTransientFaultRetryPolicyFactory : ISqlServerTransientFaultRetryPolicyFactory
        {
            private readonly IAsyncPolicy _retryPolicy;

            public SqlServerTransientFaultRetryPolicyFactory(
                SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration)
            {
                EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));

                SqlServerTransientFaultRetryPolicyConfiguration transientFaultRetryPolicyConfiguration = sqlServerDataStoreConfiguration.TransientFaultRetryPolicy;

                IEnumerable<TimeSpan> sleepDurations = Backoff.ExponentialBackoff(
                    transientFaultRetryPolicyConfiguration.InitialDelay,
                    transientFaultRetryPolicyConfiguration.RetryCount,
                    transientFaultRetryPolicyConfiguration.Factor,
                    transientFaultRetryPolicyConfiguration.FastFirst);

                PolicyBuilder policyBuilder = Policy
                    .Handle<SqlException>(sqlException => sqlException.IsTransient())
                    .Or<TimeoutException>();

                _retryPolicy = policyBuilder.WaitAndRetryAsync(sleepDurations);
            }

            /// <inheritdoc/>
            public IAsyncPolicy Create()
                => _retryPolicy;
        }

        #endregion
    }
}
