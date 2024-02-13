// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------


namespace Microsoft.Health.Dicom.Core.Features.Telemetry;

public interface IDicomTelemetryClient
{
    void TrackMetric(string name, int value);

    void TrackMetric(string name, long value);
}
