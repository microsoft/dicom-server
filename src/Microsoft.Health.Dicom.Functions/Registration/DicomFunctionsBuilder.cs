// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Core.Registration;

namespace Microsoft.Health.Dicom.Functions.Registration
{
    internal class DicomFunctionsBuilder : IDicomFunctionsBuilder
    {
        public DicomFunctionsBuilder(IServiceCollection services)
            => Services = EnsureArg.IsNotNull(services, nameof(services));

        public IServiceCollection Services { get; }
    }
}
