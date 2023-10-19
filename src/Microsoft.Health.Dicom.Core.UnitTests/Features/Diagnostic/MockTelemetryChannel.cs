// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Diagnostic;

internal class MockTelemetryChannel : ITelemetryChannel
{
    public IList<ITelemetry> Items { get; private set; } = new List<ITelemetry>();

    public void Send(ITelemetry item)
    {
        Items.Add(item);
    }

    public void Flush()
    {
        throw new NotImplementedException();
    }

    public bool? DeveloperMode { get; set; }
    public string EndpointAddress { get; set; }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}