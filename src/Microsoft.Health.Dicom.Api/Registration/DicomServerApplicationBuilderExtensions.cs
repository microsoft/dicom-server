// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;

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
