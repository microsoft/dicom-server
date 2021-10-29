// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class JsonServiceCollectionExtensions
    {
        public static IServiceCollection AddDicomJsonNetSerialization(this IServiceCollection services, bool autoValidation = false)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            // Register the Json Serializer to use
            var jsonSerializer = new JsonSerializer();
            jsonSerializer.Converters.Add(new JsonDicomConverter());
            services.AddSingleton(jsonSerializer);

            // Disable fo-dicom data item validation. Disabling at global level
            // Opt-in validation instead of opt-out
            // De-serializing to Dataset while read has no Dataset level option to disable validation
#pragma warning disable CS0618 // Type or member is obsolete
            DicomValidation.AutoValidation = autoValidation;
#pragma warning restore CS0618 // Type or member is obsolete

            return services;
        }
    }
}
