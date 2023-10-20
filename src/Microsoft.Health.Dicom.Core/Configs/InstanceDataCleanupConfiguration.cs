// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Configs;

public class InstanceDataCleanupConfiguration
{
    /// <summary>
    /// Gets or sets the operation id
    /// </summary>
    public Guid OperationId { get; set; } = Guid.Parse("f0a54b2a-eeca-4c45-af90-52ac15f6d486");

    /// <summary>
    /// Gets or sets the start time stamp for clean up
    /// </summary>
    public DateTimeOffset StartTimeStamp { get; set; } = new DateTimeOffset(new DateTime(2023, 06, 01, 0, 0, 0), TimeSpan.Zero);

    /// <summary>
    /// Gets or sets the end time stamp for clean up
    /// </summary>
    public DateTimeOffset EndTimeStamp { get; set; } = new DateTimeOffset(new DateTime(2023, 09, 30, 0, 0, 0), TimeSpan.Zero);
}
