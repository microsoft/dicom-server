// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;

namespace Microsoft.Health.Dicom.Core.Registration
{
    public static class DicomFunctionsBuilderCoreRegistrationExtensions
    {
        public static IDicomFunctionsBuilder AddCoreComponents(
            this IDicomFunctionsBuilder builder)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));
            var services = builder.Services;
            services.AddScopedDefault<InstanceReindexer>();
            services.AddScopedDefault<AddExtendedQueryTagService>();
            services.AddSingletonDefault<DicomTagParser>();
            services.AddSingletonDefault<ExtendedQueryTagEntryValidator>();

            return builder;
        }

    }
}
