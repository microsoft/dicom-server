// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Net.Mime;
using EnsureThat;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Configs;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Builder
{
    public static class DicomServerApplicationBuilderExtensions
    {
        private const string OhifViewerIndexPagePath = "index.html";

        /// <summary>
        /// Adds DICOM server functionality to the pipeline.
        /// </summary>
        /// <param name="app">The application builder instance.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseDicomServer(this IApplicationBuilder app)
        {
            EnsureArg.IsNotNull(app, nameof(app));

            app.UseMvc();

            app.UseHealthChecks(new PathString(KnownRoutes.HealthCheck), new HealthCheckOptions
            {
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
                            }),
                        });

                    httpContext.Response.ContentType = MediaTypeNames.Application.Json;
                    await httpContext.Response.WriteAsync(response);
                },
            });

            var featureConfiguration = app.ApplicationServices.GetRequiredService<IOptions<FeatureConfiguration>>();

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

            return app;
        }
    }
}
