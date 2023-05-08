// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Configs;

public class FrameRangeMigrationConfiguration
{
    /// <summary>
    /// Gets or sets the frame range migration operation id
    /// </summary>
    public Guid OperationId { get; set; } = Guid.Parse("dde170a7-5783-4704-b734-bebf349abcfa");

    /// <summary>
    /// Gets or sets the start time stamp for migration
    /// </summary>
    public DateTimeOffset StartTimeStamp { get; set; } = new DateTime(2023, 04, 10, 0, 0, 0);

    /// <summary>
    /// Gets or sets the end time stamp for migration
    /// </summary>
    public DateTimeOffset EndTimeStamp { get; set; } = new DateTime(2023, 04, 17, 0, 0, 0);
}
