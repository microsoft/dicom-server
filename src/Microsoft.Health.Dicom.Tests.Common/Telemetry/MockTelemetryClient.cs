// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Health.Dicom.Tests.Common.Telemetry;

public class MockTelemetryClient
{
    public static (TelemetryClient, MockTelemetryChannel) CreateTelemetryClientWithChannel()
    {
        MockTelemetryChannel channel = new MockTelemetryChannel();

        TelemetryConfiguration configuration = new TelemetryConfiguration
        {
            TelemetryChannel = channel,
#pragma warning disable CS0618 // Type or member is obsolete
            InstrumentationKey = Guid.NewGuid().ToString()
#pragma warning restore CS0618 // Type or member is obsolete
        };
        configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

        return (new TelemetryClient(configuration), channel);
    }
}