// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Health.Development.IdentityProvider.Registration;

namespace Microsoft.Health.Dicom.Web
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            IWebHost host = WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, builder) => builder.AddDevelopmentAuthEnvironmentIfConfigured(builder.Build(), "DicomServer"))
                .ConfigureKestrel(option => option.Limits.MaxRequestBodySize = int.MaxValue) // When hosted on Kestrel, it's allowed to upload >2GB file, set to 2GB by default
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
