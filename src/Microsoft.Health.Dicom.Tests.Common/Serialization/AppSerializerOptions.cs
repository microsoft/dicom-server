// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using FellowOakDicom.Serialization;

namespace Microsoft.Health.Dicom.Tests.Common.Serialization
{
    public static class AppSerializerOptions
    {
        public static JsonSerializerOptions Json { get; private set; }

        static AppSerializerOptions()
        {
            Json = new JsonSerializerOptions();
            Json.Converters.Add(new DicomJsonConverter());
        }
    }
}
