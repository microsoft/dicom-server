// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection
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
            app.UseStaticFiles();
            app.UseMvc();
            return app;
        }
    }
}
