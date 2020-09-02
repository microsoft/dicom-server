// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.DicomCast.Core.Configurations
{
    /// <summary>
    /// The configuration related to <see cref="Features.Worker.DicomCastWorker"/>.
    /// </summary>
    public class DicomCastWorkerConfiguration
    {
        /// <summary>
        /// The period of time to wait before polling new changes feed from DICOMWeb when previous poll indicates there are no more new changes.
        /// </summary>
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The period of time to wait before polling new changes feed from DICOMWeb when previous poll indicates there are potentially new changes.
        /// </summary>
        public TimeSpan PollIntervalDuringCatchup { get; set; } = TimeSpan.Zero;
    }
}
