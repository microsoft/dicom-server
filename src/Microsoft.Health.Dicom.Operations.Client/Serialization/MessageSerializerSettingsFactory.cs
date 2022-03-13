// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Operations.Client.Serialization;

// TODO: Migrate to common package
internal class MessageSerializerSettingsFactory : IMessageSerializerSettingsFactory
{
    public JsonSerializerSettings CreateJsonSerializerSettings()
    {
        // Based on the framework settings:
        // https://github.com/Azure/azure-functions-durable-extension/blob/v2.6.0/src/WebJobs.Extensions.DurableTask/MessageSerializerSettingsFactory.cs
        return new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            DateParseHandling = DateParseHandling.None,
        };
    }
}
