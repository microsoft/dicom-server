// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;

namespace Microsoft.AspNetCore.Builder
{
    public static class DicomServerApplicationBuilderExtensions
    {
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
                var options = new DefaultFilesOptions();

                options.DefaultFileNames.Clear();
                options.DefaultFileNames.Add("index.html");

                app.UseDefaultFiles(options);
                app.UseStaticFiles();
            }

#pragma warning disable CS0618 // Type or member is obsolete
            DicomValidation.AutoValidation = featureConfiguration.Value.EnableDicomAutoValidation;
#pragma warning restore CS0618 // Type or member is obsolete

            return app;
        }
    }
}
