// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Net.Mime;
using EnsureThat;
using FellowOakDicom;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Registration;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Configs;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Builder;

public static class DicomServerApplicationBuilderExtensions
{
    private const string OhifViewerIndexPagePath = "index.html";

    /// <summary>
    /// Adds DICOM server functionality to the pipeline.
    /// </summary>
    /// <param name="app">The application builder instance.</param>
    /// <param name="useDevelopmentIdentityProvider">The method used to register the development identity provider.</param>
    /// <param name="useHttpLoggingMiddleware">The method used to register the http logging middleware.</param>
    /// <param name="healthCheckOptionsPredicate">The predicate used to filter health check services.</param>
    /// <param name="mapAdditionalEndpoints">The method used to register additional endpoints.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseDicomServer(
        this IApplicationBuilder app,
        Func<IApplicationBuilder, IApplicationBuilder> useDevelopmentIdentityProvider = null,
        Func<IApplicationBuilder, IApplicationBuilder> useHttpLoggingMiddleware = null,
        Func<HealthCheckRegistration, bool> healthCheckOptionsPredicate = null,
        Func<IEndpointRouteBuilder, IEndpointRouteBuilder> mapAdditionalEndpoints = null)
    {
        EnsureArg.IsNotNull(app, nameof(app));

        app.UseQueryStringValidator();

        app.UseHttpsRedirection();

        app.UseCachedHealthChecks(new PathString(KnownRoutes.HealthCheck));

        // Update Fellow Oak DICOM services to use ASP.NET Core's service container
        DicomSetupBuilder.UseServiceProvider(app.ApplicationServices);

        IOptions<FeatureConfiguration> featureConfiguration = app.ApplicationServices.GetRequiredService<IOptions<FeatureConfiguration>>();
        if (featureConfiguration.Value.EnableOhifViewer)
        {
            // In order to make OHIF viewer work with direct link to studies, we need to rewrite any path under viewer
            // back to the index page so the viewer can display accordingly.
            RewriteOptions rewriteOptions = new RewriteOptions()
                .AddRewrite("^viewer/(.*?)", OhifViewerIndexPagePath, true);

            app.UseRewriter(rewriteOptions);

            var options = new DefaultFilesOptions();

            options.DefaultFileNames.Clear();
            options.DefaultFileNames.Add(OhifViewerIndexPagePath);

            app.UseDefaultFiles(options);
            app.UseStaticFiles();
        }

        app.UseRouting();
        useDevelopmentIdentityProvider?.Invoke(app);
        useHttpLoggingMiddleware?.Invoke(app);

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks(KnownRoutes.HealthCheck,
                new HealthCheckOptions
                {
                    Predicate = healthCheckOptionsPredicate,
                    ResponseWriter = async (httpContext, healthReport) =>
                    {
                        var response = JsonConvert.SerializeObject(
                            new
                            {
                                overallStatus = healthReport.Status.ToString(),
                                details = healthReport.Entries.Select(entry => new
                                {
                                    name = entry.Key,
                                    status = Enum.GetName(typeof(HealthStatus), entry.Value.Status),
                                    description = entry.Value.Description,
                                    data = entry.Value.Data,
                                }),
                            });
                        httpContext.Response.ContentType = MediaTypeNames.Application.Json;
                        await httpContext.Response.WriteAsync(response).ConfigureAwait(false);
                    }
                });

            mapAdditionalEndpoints?.Invoke(endpoints);
        });

        return app;
    }
}
