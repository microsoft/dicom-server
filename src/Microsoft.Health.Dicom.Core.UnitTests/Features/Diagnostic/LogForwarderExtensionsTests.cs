// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Health.Dicom.Core.Features.Diagnostic;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Diagnostic;

public class LogForwarderExtensionsTests
{
    [Fact]
    public void GivenClientUsingForwardTelemetry_ExpectForwardLogFlagIsSetWithNoAdditionalProperties()
    {
        (TelemetryClient telemetryClient, var channel) = CreateTelemetryClientWithChannel();
        telemetryClient.ForwardLogTrace("A message");

        Assert.Single(channel.Items);
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Single(channel.Items[0].Context.Properties);
        Assert.Equal(Boolean.TrueString, channel.Items[0].Context.Properties["forwardLog"]);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Fact]
    public void GivenClientUsingForwardTelemetryWithIdentifier_ExpectForwardLogFlagIsSetWithAdditionalProperties()
    {
        (TelemetryClient telemetryClient, var channel) = CreateTelemetryClientWithChannel();

        var expectedIdentifier = new InstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(),
            TestUidGenerator.Generate(), Partition.Default);
        telemetryClient.ForwardLogTrace("A message", expectedIdentifier);

        Assert.Single(channel.Items);
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Equal(5, channel.Items[0].Context.Properties.Count);
        Assert.Equal(expectedIdentifier.SopInstanceUid,
            channel.Items[0].Context.Properties["dicomAdditionalInformation_sopInstanceUID"]);
        Assert.Equal(expectedIdentifier.SeriesInstanceUid,
            channel.Items[0].Context.Properties["dicomAdditionalInformation_seriesInstanceUID"]);
        Assert.Equal(expectedIdentifier.StudyInstanceUid,
            channel.Items[0].Context.Properties["dicomAdditionalInformation_studyInstanceUID"]);
        Assert.Equal(expectedIdentifier.Partition.Name,
            channel.Items[0].Context.Properties["dicomAdditionalInformation_partitionName"]);
        Assert.Equal(Boolean.TrueString, channel.Items[0].Context.Properties["forwardLog"]);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Fact]
    public void GivenClientUsingForwardTelemetry_whenForwardOperationLogTraceWithSizeLimit_ExpectForwardLogFlagIsSet()
    {
        (TelemetryClient telemetryClient, var channel) = CreateTelemetryClientWithChannel();

        var operationId = Guid.NewGuid().ToString();
        var input = "input";
        var message = "a message";
        telemetryClient.ForwardOperationLogTrace(message, operationId, input);

        Assert.Single(channel.Items);
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Equal(3, channel.Items[0].Context.Properties.Count);
        Assert.Equal(Boolean.TrueString, channel.Items[0].Context.Properties["forwardLog"]);
        Assert.Equal(operationId, channel.Items[0].Context.Properties["dicomAdditionalInformation_operationId"]);
        Assert.Equal(input, channel.Items[0].Context.Properties["dicomAdditionalInformation_input"]);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Fact]
    public void GivenClientUsingForwardTelemetry_whenForwardOperationLogTraceWithSizeLimitExceeded_ExpectForwardLogFlagIsSetAndMultipleTelemetriesEmitted()
    {
        (TelemetryClient telemetryClient, var channel) = CreateTelemetryClientWithChannel();

        var operationId = Guid.NewGuid().ToString();
        var expectedFirstItemInput = "a".PadRight(32 * 1024); // split occurs at 32 kb
        var expectedSecondItemInput = "b".PadRight(32 * 1024); // split occurs at 32 kb
        var fullInput = expectedFirstItemInput + expectedSecondItemInput;
        var message = "a message";
        telemetryClient.ForwardOperationLogTrace(message, operationId, fullInput);

        Assert.Equal(2, channel.Items.Count);

#pragma warning disable CS0618 // Type or member is obsolete
        var firstItem = channel.Items[0];
        Assert.Equal(3, firstItem.Context.Properties.Count);
        Assert.Equal(Boolean.TrueString, firstItem.Context.Properties["forwardLog"]);
        Assert.Equal(operationId, firstItem.Context.Properties["dicomAdditionalInformation_operationId"]);
        Assert.Equal(expectedFirstItemInput, firstItem.Context.Properties["dicomAdditionalInformation_input"]);

        var secondItem = channel.Items[1];
        Assert.Equal(3, secondItem.Context.Properties.Count);
        Assert.Equal(Boolean.TrueString, secondItem.Context.Properties["forwardLog"]);
        Assert.Equal(operationId, secondItem.Context.Properties["dicomAdditionalInformation_operationId"]);
        Assert.Equal(expectedSecondItemInput, secondItem.Context.Properties["dicomAdditionalInformation_input"]);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    private static (TelemetryClient, MockTelemetryChannel) CreateTelemetryClientWithChannel()
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