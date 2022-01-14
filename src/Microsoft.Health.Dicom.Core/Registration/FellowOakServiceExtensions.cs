// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using FellowOakDicom.Imaging.NativeCodec;
using Microsoft.Health.Dicom.Core.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class FellowOakServiceExtensions
    {
        public static IServiceCollection AddFellowOakDicomServices(this IServiceCollection services, bool skipValidation = false)
        {
            if (skipValidation)
            {
                // Note: this is an extension method, but it isn't stateful.
                // Instead it modifies a static property, so we'll change the invocation to look more appropriate
                DicomValidationBuilderExtension.SkipValidation(null);
            }

            services
                .AddFellowOakDicom()
                .AddTranscoderManager<NativeTranscoderManager>()
                .AddLogManager<FellowOakDecoratorLogManager>();

            return services;
        }
    }
}
