// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;

namespace Microsoft.Health.Dicom.Api.Registration;

public static class OhifViewerExtensions
{
    private const string OhifViewerIndexPagePath = "index.html";

    /// <summary>
    /// Enable OHIF viewer.
    /// </summary>
    public static void UseOhifViewer(this IApplicationBuilder app)
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
}
