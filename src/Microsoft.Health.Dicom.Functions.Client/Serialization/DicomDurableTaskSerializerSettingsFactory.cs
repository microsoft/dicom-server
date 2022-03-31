// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Operations.Functions.DurableTask;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Functions.Client.Serialization;

internal class DicomDurableTaskSerializerSettingsFactory : IMessageSerializerSettingsFactory
{
    // TODO: Unseal class
    private static readonly DurableTaskSerializerSettingsFactory BaseFactory = new DurableTaskSerializerSettingsFactory();

    public JsonSerializerSettings CreateJsonSerializerSettings()
    {
        JsonSerializerSettings settings = BaseFactory.CreateJsonSerializerSettings();
        settings.ConfigureDefaultDicomSettings();

        return settings;
    }
}
