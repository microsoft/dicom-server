// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Jobs;

[assembly: FunctionsStartup(typeof(Startup))]
namespace Microsoft.Health.Dicom.Jobs
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            EnsureArg.IsNotNull(builder);

            builder.Services.AddSingleton<IRepository, Repository>();
            builder.Services.AddLogging();
        }
    }
    public interface IRepository
    {
        string GetData();
    }

    public class Repository : IRepository
    {
        public string GetData()
        {
            return "some data!";
        }
    }
}
